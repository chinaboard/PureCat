using System;
using System.Collections.Generic;

namespace PureCat.Configuration
{
    /// <summary>
    ///   Cat客户端配置
    /// </summary>
    public class ClientConfig
    {
        private List<Server> _mServers;
        private Domain _mDomain;
        private Random _mRandom;

        public ClientConfig(Domain domain = null, params Server[] serverList)
        {
            _mRandom = new Random();
            _mServers = new List<Server>();
            _mDomain = domain ?? new Domain();
            if (serverList != null && serverList.Length != 0)
                _mServers.AddRange(serverList);
            RandomServer();
        }

        public Domain Domain
        {
            get { return _mDomain ?? (_mDomain = new Domain()); }

            set { _mDomain = value; }
        }

        /// <summary>
        ///   Cat日志服务器，可以有多个
        /// </summary>
        public List<Server> Servers
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