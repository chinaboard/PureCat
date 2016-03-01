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
        private int _errors;
        private bool _active;
        private readonly int _maxQueueSize = 100000;

        public TcpMessageSender(ClientConfig clientConfig, IMessageStatistics statistics)
        {
            _clientConfig = clientConfig;
            _statistics = statistics;
            _connPool = new ConcurrentDictionary<Server, TcpClient>();
            _queue = new ConcurrentQueue<IMessageTree>();
            _codec = new PlainTextMessageCodec();
        }

        #region IMessageSender Members

        public virtual bool HasSendingMessage
        {
            get { return _queue.Count > 0; }
        }

        public void Initialize()
        {
            _clientConfig.Servers.ForEach(server =>
            {
                _connPool[server] = CreateChannel(server);
            });

            _active = true;

            ThreadPool.QueueUserWorkItem(ChannelManagementTask);
            ThreadPool.QueueUserWorkItem(AsynchronousSendTask);

            Logger.Info("Thread(TcpMessageSender-ChannelManagementTask) started.");
            Logger.Info("Thread(TcpMessageSender-AsynchronousSendTask) started.");
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
                    _errors++;

                    if (_statistics != null)
                    {
                        _statistics.OnOverflowed(tree);
                    }

                    if (_errors % 100 == 0)
                    {
                        Logger.Warn("Can't send message to cat-server due to queue's full! Count: " + _errors);
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

        public void ChannelManagementTask(object o)
        {
            while (true)
            {
                if (_active)
                {
                    _connPool.ToList().ForEach(kvp =>
                    {
                        if (!kvp.Value.Connected)
                            _connPool[kvp.Key] = CreateChannel(kvp.Key);
                    });
                }
                Thread.Sleep(5 * 1000); // every 2 seconds
            }
        }

        public void AsynchronousSendTask(object o)
        {
            while (true)
            {
                if (_active)
                {
                    var activeChannel = _connPool.ToList()[_rand.Next(_clientConfig.Servers.Count)].Value;

                    while (_queue.Count == 0 || !activeChannel.Connected)
                    {
                        Thread.Sleep(500);
                    }

                    IMessageTree tree = null;
                    _queue.TryDequeue(out tree);

                    try
                    {
                        SendInternal(tree, activeChannel);
                        if (tree != null) tree.Message = null;
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
                ChannelBuffer buf = new ChannelBuffer(8192);

                _codec.Encode(tree, buf);

                byte[] data = buf.ToArray();

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

            TcpClient socket = new TcpClient();

            socket.NoDelay = true;
            socket.ReceiveTimeout = 2 * 1000; // 2 seconds

            string ip = server.Ip;
            int port = server.Port;

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
                Logger.Error(
                    "Failed to connect to server({0}:{1}). Error: {2}.",
                    ip,
                    port,
                    e.Message
                    );
            }

            return null;
        }
    }
}