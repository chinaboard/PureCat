using System;
using PureCat.Configuration;
using PureCat.Message.Spi.Codec;
using PureCat.Util;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;

namespace PureCat.Message.Spi.IO
{
    public class TcpMessageSender : IMessageSender
    {
        private readonly ClientConfig _mClientConfig;
        private readonly IMessageCodec _mCodec;
        private readonly ConcurrentQueue<IMessageTree> _mQueue;
        private readonly IMessageStatistics _mStatistics;
        private bool _mActive;
        private TcpClient _mActiveChannel;
        private int _mActiveIndex;
        private int _mErrors;
        private TcpClient _mLastChannel;
        private readonly int _maxQueueSize = 100000;

        public TcpMessageSender(ClientConfig clientConfig, IMessageStatistics statistics)
        {
            _mClientConfig = clientConfig;
            _mStatistics = statistics;
            _mActive = true;
            _mQueue = new ConcurrentQueue<IMessageTree>();
            _mCodec = new PlainTextMessageCodec();
        }

        #region IMessageSender Members

        public virtual bool HasSendingMessage
        {
            get { return _mQueue.Count > 0; }
        }

        public void Initialize()
        {
            int len = _mClientConfig.Servers.Count;

            for (int i = 0; i < len; i++)
            {
                TcpClient channel = CreateChannel(i);

                if (channel != null)
                {
                    _mActiveChannel = channel;
                    _mActiveIndex = i;
                    break;
                }
            }

            //TODO:连接管理做好
            ThreadPool.QueueUserWorkItem(ChannelManagementTask);
            ThreadPool.QueueUserWorkItem(AsynchronousSendTask);

            Logger.Info("Thread(TcpMessageSender-ChannelManagementTask) started.");
            Logger.Info("Thread(TcpMessageSender-AsynchronousSendTask) started.");
        }

        public void Send(IMessageTree tree)
        {
            lock (_mQueue)
            {
                if (_mQueue.Count < _maxQueueSize)
                {
                    _mQueue.Enqueue(tree);
                }
                else
                {
                    // throw it away since the queue is full
                    _mErrors++;

                    if (_mStatistics != null)
                    {
                        _mStatistics.OnOverflowed(tree);
                    }

                    if (_mErrors % 100 == 0)
                    {
                        Logger.Warn("Can't send message to cat-server due to queue's full! Count: " + _mErrors);
                    }
                }
            }
        }

        public void Shutdown()
        {
            _mActive = false;

            try
            {
                if (_mActiveChannel != null && _mActiveChannel.Connected)
                {
                    _mActiveChannel.Close();
                }
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
                if (_mActive)
                {
                    if (_mActiveChannel == null || !_mActiveChannel.Connected)
                    {
                        if (_mActiveChannel != null)
                            Logger.Warn("ChannelManagementTask中，Socket关闭");
                        _mActiveIndex = _mClientConfig.Servers.Count;
                    }

                    for (int i = 0; i < _mActiveIndex; i++)
                    {
                        TcpClient channel = CreateChannel(i);

                        if (channel != null)
                        {
                            _mLastChannel = _mActiveChannel;
                            _mActiveChannel = channel;
                            _mActiveIndex = i;
                            break;
                        }
                    }
                }

                Thread.Sleep(5 * 1000); // every 2 seconds
            }
        }

        public void AsynchronousSendTask(object o)
        {
            while (true)
            {
                if (_mActive)
                {
                    while (_mQueue.Count == 0 || _mActiveChannel == null || !_mActiveChannel.Connected)
                    {
                        if (_mActiveChannel != null && !_mActiveChannel.Connected)
                            Logger.Warn("AsynchronousSendTask中，Socket关闭");
                        Thread.Sleep(500);
                    }

                    IMessageTree tree = null;
                    _mQueue.TryDequeue(out tree);

                    try
                    {
                        SendInternal(tree);
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

        private void SendInternal(IMessageTree tree)
        {
            if (_mLastChannel != null)
            {
                try
                {
                    Logger.Warn("SendInternal中，_mLastChannel关闭");
                    _mLastChannel.Close();
                }
                catch
                {
                    // ignore it
                }

                _mLastChannel = null;
            }

            if (_mActiveChannel != null && _mActiveChannel.Connected)
            {
                ChannelBuffer buf = new ChannelBuffer(8192);

                _mCodec.Encode(tree, buf);

                byte[] data = buf.ToArray();

                _mActiveChannel.Client.Send(data);

                if (_mStatistics != null)
                {
                    _mStatistics.OnBytes(data.Length);
                }
            }
            else
            {
                Logger.Warn("SendInternal中，Socket关闭");
            }
        }

        private TcpClient CreateChannel(int index)
        {
            Server server = _mClientConfig.Servers[index];

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