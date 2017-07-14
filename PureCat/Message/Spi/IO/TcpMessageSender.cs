using System;
using PureCat.Configuration;
using PureCat.Message.Spi.Codec;
using PureCat.Util;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using PureCat.Message.Internals;

namespace PureCat.Message.Spi.IO
{
    public class TcpMessageSender : IMessageSender
    {
        private static Random _rand = new Random();

        private readonly ClientConfig _clientConfig;
        private readonly IMessageCodec _codec;
        private readonly ConcurrentQueue<IMessageTree> _queue;
        private readonly ConcurrentQueue<IMessageTree> _atomicTress;
        private readonly ConcurrentDictionary<Server, TcpClient> _connPool;
        private readonly IMessageStatistics _statistics;
        private long _errors;
        private bool _active;
        private readonly int _maxQueueSize;

        public TcpMessageSender(ClientConfig clientConfig, IMessageStatistics statistics)
        {
            _clientConfig = clientConfig;
            _statistics = statistics;
            _connPool = new ConcurrentDictionary<Server, TcpClient>();
            _queue = new ConcurrentQueue<IMessageTree>();
            _atomicTress = new ConcurrentQueue<IMessageTree>();
            _codec = new PlainTextMessageCodec();
            _maxQueueSize = clientConfig.Domain.MaxQueueSize;
        }

        public virtual bool HasSendingMessage => _queue.Count > 0 && _connPool.Count > 0;

        public void Initialize()
        {
            _active = true;

#if NET40
            TaskEx.Run(ServerManagementTask);
#else
            Task.Run(ServerManagementTask);
#endif
            Logger.Info("Thread(ServerManagementTask) started.");

#if NET40
            TaskEx.Run(ChannelManagementTask);
#else
            Task.Run(ChannelManagementTask);
#endif
            Logger.Info("Thread(ChannelManagementTask) started.");

            for (var i = 0; i < _clientConfig.Domain.ThreadPool; i++)
            {
#if NET40
                TaskEx.Run(() => AsynchronousSendTask(i));
#else
                Task.Run(() => AsynchronousSendTask(i));
#endif
                Logger.Info($"Thread(AsynchronousSendTask-{i}) started.");
            }

#if NET40
            TaskEx.Run(MergeAtomicTask);
#else
            Task.Run(MergeAtomicTask);
#endif
            Logger.Info("Thread(MergeAtomicTask) started.");
        }

        public void Send(IMessageTree tree)
        {
            if (tree == null)
                return;
            if (IsAtomicMessage(tree))
            {
                if (_atomicTress.Count < _maxQueueSize)
                {
                    _atomicTress.Enqueue(tree);
                }
                else
                {
                    LogQueueFullInfo("AtomicMessage");
                }
            }
            else
            {
                if (_queue.Count < _maxQueueSize)
                {
                    _queue.Enqueue(tree);
                }
                else
                {
                    LogQueueFullInfo("Message");
                }
            }
        }

        public void Shutdown()
        {
            _active = false;
        }

        public async Task ServerManagementTask()
        {
            while (true)
            {
                if (_active)
                {
                    await _clientConfig.Initialize();
                    _clientConfig.Servers.ForEach(server =>
                    {
                        if (!_connPool.ContainsKey(server))
                            _connPool.GetOrAdd(server, _ => CreateChannel(server));
                    });
                }

#if NET40
                await TaskEx.Delay(60 * 1000); // every 60 seconds
#else
                await Task.Delay(60 * 1000); // every 60 seconds
#endif
            }
        }

        public async Task ChannelManagementTask()
        {
            while (true)
            {
                if (_active && _connPool.Count != 0)
                {
                    var connPoolList = _connPool.ToList();
                    connPoolList.ForEach(kvp =>
                    {
                        if (kvp.Value == null || !kvp.Value.Connected)
                            _connPool[kvp.Key] = CreateChannel(kvp.Key);
                    });
                }

#if NET40
                await TaskEx.Delay(5 * 1000); // every 5 seconds
#else
                await Task.Delay(5 * 1000); // every 5 seconds
#endif
            }
        }

        public async Task AsynchronousSendTask(int i)
        {
            while (true)
            {
                if (_active)
                {
                    try
                    {
                        TcpClient activeChannel = null;

                        if (_connPool.Count != 0)
                        {
                            Interlocked.Exchange(ref activeChannel, _connPool.Values.ToList()[i % _connPool.Count]);
                        }
                        else
                        {
#if NET40
                            await TaskEx.Delay(100);
#else
                            await Task.Delay(100);
#endif
                            continue;
                        }
                        while (_queue.Count == 0 || activeChannel == null || !activeChannel.Connected)
                        {
#if NET40
                            await TaskEx.Delay(500);
#else
                            await Task.Delay(500);
#endif
                            Interlocked.Exchange(ref activeChannel, _connPool.Values.ToList()[i % _connPool.Count]);
                        }

                        if (_queue.TryDequeue(out IMessageTree tree))
                        {
                            if (tree != null)
                            {
                                await SendInternal(tree, activeChannel);
                                tree.Message = null;
                            }
                        }
                    }
                    catch (Exception t)
                    {
                        Logger.Error("Error when sending message over TCP socket! Error: {0}", t);
                    }
                }
                else
                {
#if NET40
                    await TaskEx.Delay(5 * 1000);
#else
                    await Task.Delay(5 * 1000);
#endif
                }
            }
        }

        public async Task MergeAtomicTask()
        {
            while (true)
            {
                if (ShouldMerge())
                {
                    var tree = MergeTree();
                    if (tree == null)
                    {
#if NET40
                        await TaskEx.Delay(5000);
#else
                        await Task.Delay(5000);
#endif
                        continue;
                    }
                    Send(tree);
                }
                else
                {
#if NET40
                    await TaskEx.Delay(100);
#else
                    await Task.Delay(100);
#endif
                }
            }
        }

        private async Task SendInternal(IMessageTree tree, TcpClient activeChannel)
        {
            if (activeChannel != null && activeChannel.Connected)
            {
                var buf = new ChannelBuffer(8192);

                _codec.Encode(tree, buf);

                var data = buf.ToArray();

                await Task.Factory.FromAsync((buffer, callback, state) =>
                        activeChannel.Client.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, callback, state),
                    activeChannel.Client.EndSend,
                    data,
                    null);

                _statistics?.OnBytes(data.Length);
            }
            else
            {
                Logger.Warn("SendInternal中，Socket关闭");
            }
        }

        private TcpClient CreateChannel(Server server)
        {
            if (!server.Enabled)
            {
                return null;
            }

            var socket = new TcpClient() { NoDelay = true, ReceiveTimeout = 2000 };

            var ip = server.Ip;
            var port = server.Port;

            Logger.Info("Connecting to server({0}:{1}) ...", ip, port);

            try
            {
                socket.Connect(ip, port);

                if (socket.Connected)
                {
                    Logger.Info("Connected to server({0}:{1}).", ip, port);

                    return socket;
                }
                Logger.Error("Failed to connect to server({0}:{1}).", ip, port);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to connect to server({0}:{1}). Error: {2}.", ip, port, e.Message);
            }

            return null;
        }

        private bool ShouldMerge()
        {
            if (!_atomicTress.TryDequeue(out IMessageTree tree))
            {
                return false;
            }

            var firstTime = tree.Message.Timestamp;
            var maxDuration = 1000 * 30;

            return MilliSecondTimer.CurrentTimeMillis - firstTime > maxDuration || _atomicTress.Count >= PureCatConstants.MAX_CHILD_NUMBER;
        }

        private IMessageTree MergeTree()
        {
            var max = PureCatConstants.MAX_CHILD_NUMBER;
            var tran = new DefaultTransaction("_CatMergeTree", "_CatMergeTree");
            if (!_atomicTress.TryDequeue(out IMessageTree first))
            {
                return null;
            }

            tran.Status = PureCatConstants.SUCCESS;
            tran.Complete();
            tran.AddChild(first.Message);
            tran.Timestamp = first.Message.Timestamp;

            long lastTimestamp = 0;
            long lastDuration = 0;

            while (max-- >= 0)
            {
                if (!_atomicTress.TryDequeue(out IMessageTree tree))
                {
                    tran.DurationInMillis = (lastTimestamp - tran.Timestamp + lastDuration);
                    break;
                }
                lastTimestamp = tree.Message.Timestamp;
                var message = tree.Message as DefaultTransaction;
                lastDuration = message?.DurationInMillis ?? 0;
                tran.AddChild(tree.Message);
            }
            first.Message = tran;
            return first;
        }

        private bool IsAtomicMessage(IMessageTree tree)
        {
            var message = tree.Message;

            if (message is ITransaction)
            {
                var type = message.Type;
                return type.StartsWith("Cache.") || type.StartsWith("SQL");
            }
            return true;
        }

        private void LogQueueFullInfo(string name)
        {
            Interlocked.Increment(ref _errors);

            _statistics?.OnOverflowed();

            if (Interlocked.Read(ref _errors) % 100 == 0)
            {
                Logger.Warn($"{name} queue's full! Count: " + Interlocked.Read(ref _errors));
            }
        }
    }
}