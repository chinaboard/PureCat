using System;
using System.Collections.Generic;

namespace PureCat.Configuration
{
    /// <summary>
    ///   Cat客户端配置
    /// </summary>
    public class ClientConfig
    {
        private readonly IList<Server> _mServers;
        private Domain _mDomain;
        private Random _mRandom;

        public ClientConfig(Domain domain = null, params string[] serverList)
        {
            _mRandom = new Random();
            _mServers = new List<Server>();
            _mDomain = domain ?? new Domain();
            if (serverList != null && serverList.Length != 0)
                foreach (var ip in serverList)
                    _mServers.Add(new Server(ip));
            RandomServer();
        }

        /// <summary>
        ///   是否是开发模式
        /// </summary>
        public bool DevMode { get; set; }

        public Domain Domain
        {
            get { return _mDomain ?? (_mDomain = new Domain()); }

            set { _mDomain = value; }
        }

        /// <summary>
        ///   Cat日志服务器，可以有多个
        /// </summary>
        public IList<Server> Servers
        {
            get { return _mServers; }
        }
        private void RandomServer()
        {
            if (_mServers == null || _mServers.Count < 2)
                return;
            int k = 0;
            int index = 0;
            Server tmpServer = null;
            for (int i = 0; i < _mServers.Count * 3; i++)
            {
                index = i % _mServers.Count;
                k = _mRandom.Next(_mServers.Count);
                if (k != index)
                {
                    tmpServer = _mServers[index];
                    _mServers[index] = _mServers[k];
                    _mServers[k] = tmpServer;
                }
            }

        }
    }
}