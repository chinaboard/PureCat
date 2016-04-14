using System;
using PureCat.Configuration;
using PureCat.Message.Spi.Codec;
using PureCat.Util;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
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

        public virtual bool HasSendingMessage
        {
            get { return _queue.Count > 0 && _connPool.Count > 0; }
        }

        public void Initialize()
        {
            _active = true;

            ThreadPool.QueueUserWorkItem(ServerManagementTask);
            Logger.Info("Thread(ServerManagementTask) started.");

            ThreadPool.QueueUserWorkItem(ChannelManagementTask);
            Logger.Info("Thread(ChannelManagementTask) started.");

            for (int i = 0; i < _clientConfig.Domain.ThreadPool; i++)
            {
                ThreadPool.QueueUserWorkItem(AsynchronousSendTask, i);
                Logger.Info($"Thread(AsynchronousSendTask-{i}) started.");
            }

            ThreadPool.QueueUserWorkItem(MergeAtomicTask);
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
            try
            {
                _active = false;
            }
            catch
            {
                // ignore it
            }
        }

        public void ServerManagementTask(object o)
        {
            while (true)
            {
                if (_active)
                {
                    _clientConfig.Initialize();
                    _clientConfig.Servers.ForEach(server =>
                    {
                        if (!_connPool.ContainsKey(server))
                            _connPool.GetOrAdd(server, CreateChannel(server));
                    });
                }
                Thread.Sleep(60 * 1000); // every 60 seconds
            }
        }

        public void ChannelManagementTask(object o)
        {
            while (true)
            {
                if (_active && _connPool.Count != 0)
                {
                    var connPoolList = _connPool.ToList();
                    connPoolList.ForEach(kvp =>
                    {
                        if (kvp.Value != null && !kvp.Value.Connected)
                            _connPool[kvp.Key] = CreateChannel(kvp.Key);
                    });

                }
                Thread.Sleep(5 * 1000); // every 5 seconds
            }
        }

        public void AsynchronousSendTask(object state)
        {
            var i = (int)state;
            while (true)
            {
                if (_active)
                {
                    try
                    {
                        TcpClient activeChannel = null;
                        var connPoolList = _connPool.ToList();

                        if (connPoolList.Count != 0)
                        {
                            Interlocked.Exchange(ref activeChannel, connPoolList[i % connPoolList.Count].Value);
                        }
                        else
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        while (_queue.Count == 0 || activeChannel == null || !activeChannel.Connected)
                        {
                            Thread.Sleep(500);
                            Interlocked.Exchange(ref activeChannel, connPoolList[i % connPoolList.Count].Value);
                        }

                        IMessageTree tree = null;

                        if (_queue.TryDequeue(out tree))
                        {
                            if (tree != null)
                            {
                                SendInternal(tree, activeChannel);
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
                    Thread.Sleep(5 * 1000);
                }
            }
        }

        public void MergeAtomicTask(object o)
        {
            while (true)
            {
                if (ShouldMerge())
                {
                    var tree = MergeTree();
                    if (tree == null)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }
                    else
                    {
                        Send(tree);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void SendInternal(IMessageTree tree, TcpClient activeChannel)
        {

            if (activeChannel != null && activeChannel.Connected)
            {
                var buf = new ChannelBuffer(8192);

                _codec.Encode(tree, buf);

                var data = buf.ToArray();

                activeChannel.Client.Send(data);

                if (_statistics != null)
                {
                    _statistics.OnBytes(data.Length);
                }
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
            IMessageTree tree = null;
            if (!_atomicTress.TryDequeue(out tree))
            {
                return false;
            }
            else
            {
                var firstTime = tree.Message.Timestamp;
                var maxDuration = 1000 * 30;

                if (MilliSecondTimer.CurrentTimeMillis - firstTime > maxDuration || _atomicTress.Count >= PureCatConstants.MAX_CHILD_NUMBER)
                {
                    return true;
                }
            }
            return false;
        }

        private IMessageTree MergeTree()
        {
            var max = PureCatConstants.MAX_CHILD_NUMBER;
            var tran = new DefaultTransaction("_CatMergeTree", "_CatMergeTree");
            IMessageTree first = null;
            if (!_atomicTress.TryDequeue(out first))
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
                IMessageTree tree = null;
                if (!_atomicTress.TryDequeue(out tree))
                {
                    tran.DurationInMillis = (lastTimestamp - tran.Timestamp + lastDuration);
                    break;
                }
                lastTimestamp = tree.Message.Timestamp;
                if (tree.Message is DefaultTransaction)
                {
                    lastDuration = ((DefaultTransaction)tree.Message).DurationInMillis;
                }
                else
                {
                    lastDuration = 0;
                }
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

            if (_statistics != null)
            {
                _statistics.OnOverflowed();
            }

            if (Interlocked.Read(ref _errors) % 100 == 0)
            {
                Logger.Warn($"{name} queue's full! Count: " + Interlocked.Read(ref _errors));
            }
        }
    }
}