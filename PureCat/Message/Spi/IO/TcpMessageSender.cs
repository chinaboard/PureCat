using System;
using PureCat.Configuration;
using PureCat.Message.Spi.Codec;
using PureCat.Util;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;

namespace PureCat.Message.Spi.IO
{
    public class TcpMessageSender : IMessageSender
    {
        private static Random _rand = new Random();

        private readonly ClientConfig _clientConfig;
        private readonly IMessageCodec _codec;
        private readonly ConcurrentQueue<IMessageTree> _queue;
        private readonly ConcurrentDictionary<Server, TcpClient> _connPool;
        private readonly IMessageStatistics _statistics;
        private long _errors;
        private bool _active;
        private readonly int _maxQueueSize = 100000;

        public TcpMessageSender(ClientConfig clientConfig, IMessageStatistics statistics)
        {
            _clientConfig = clientConfig;
            _statistics = statistics;
            _connPool = new ConcurrentDictionary<Server, TcpClient>();
            _queue = new ConcurrentQueue<IMessageTree>();
            _codec = new PlainTextMessageCodec();
            _maxQueueSize = clientConfig.Domain.MaxQueueSize;
        }

        #region IMessageSender Members

        public virtual bool HasSendingMessage
        {
            get { return _queue.Count > 0; }
        }

        public void Initialize()
        {
            _active = true;

            ThreadPool.QueueUserWorkItem(ServerManagementTask);
            ThreadPool.QueueUserWorkItem(ChannelManagementTask);
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                ThreadPool.QueueUserWorkItem(AsynchronousSendTask, i);
                Logger.Info($"Thread(AsynchronousSendTask-{i}) started.");
            }

            Logger.Info("Thread(ServerManagementTask) started.");
            Logger.Info("Thread(ChannelManagementTask) started.");
        }

        public void Send(IMessageTree tree)
        {
            lock (_queue)
            {
                if (_queue.Count < _maxQueueSize)
                {
                    _queue.Enqueue(tree);
                }
                else
                {
                    // throw it away since the queue is full
                    Interlocked.Increment(ref _errors);

                    if (_statistics != null)
                    {
                        _statistics.OnOverflowed(tree);
                    }

                    if (Interlocked.Read(ref _errors) % 100 == 0)
                    {
                        Logger.Warn("Can't send message to cat-server due to queue's full! Count: " + Interlocked.Read(ref _errors));
                    }
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

        #endregion

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
                if (_active)
                {
                    var connPoolList = _connPool.ToList();
                    connPoolList.ForEach(kvp =>
                    {
                        if (!kvp.Value.Connected)
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

                            SendInternal(tree, activeChannel);
                            if (tree != null) tree.Message = null;
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
    }
}