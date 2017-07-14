using PureCat.Util;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PureCat.Configuration
{
    /// <summary>
    ///   Cat客户端配置
    /// </summary>
    public class ClientConfig
    {
        private Random _random = new Random();
        private readonly object _lock = new object();

        private Server _defaultServer = null;
        private List<Server> _server = new List<Server>();

        public ClientConfig(Domain domain = null, params Server[] serverList)
        {
            Domain = domain ?? new Domain();
            if (serverList != null && serverList.Length != 0)
                Servers.AddRange(serverList);
        }

        public Domain Domain { get; set; }

        /// <summary>
        ///   Cat日志服务器，可以有多个
        /// </summary>
        public List<Server> Servers { get { return _server; } set { lock (_lock) _server = value; } }

        public async Task Initialize()
        {
            await LoadServerConfig();
            RandomServer();
        }

        public void RandomServer()
        {
            if (_server == null || _server.Count < 2)
                return;

            int k = 0;
            int index = 0;
            Server tmpServer = null;
            for (int i = 0; i < _server.Count * 3; i++)
            {
                index = i % _server.Count;
                k = _random.Next(_server.Count);
                if (k != index)
                {
                    tmpServer = _server[index];
                    _server[index] = _server[k];
                    _server[k] = tmpServer;
                }
            }
        }

        public async Task LoadServerConfig()
        {
            var serverListContent = await CatHttpRequest.GetRequest(GetServerConfigUrl());
            if (string.IsNullOrWhiteSpace(serverListContent))
            {
                return;
            }

            Logger.Info($"Get servers : {serverListContent}");

            var serverListSplit = serverListContent.TrimEnd(';').Split(';');

            var serverList = new List<Server>();

            foreach (var serverContent in serverListSplit)
            {
                try
                {
                    var content = serverContent.Split(':');
                    var ip = content[0];
                    var port = content[1];
                    //这边会有一个问题，就是http端口可能会发生变化，由于cat只返回ip，所以这个地方没法兼顾
                    serverList.Add(new Server(ip, int.Parse(port)));
                }
                catch
                {
                }
            }

            if (serverList.Count > 0)
            {
                _server = serverList;
            }
        }

        private string GetServerConfigUrl(int webPort = -1)
        {
            var serverList = _server.Where(server => server.Enabled);
            foreach (var server in serverList)
            {
                _defaultServer = _defaultServer ?? server;
                return $"http://{server.Ip}:{(webPort > 0 ? webPort : server.HttpPort)}/cat/s/router?domain={Domain.Id ?? "cat"}&ip={AppEnv.IP}";
            }
            return _defaultServer != null ? $"http://{_defaultServer.Ip}:{(webPort > 0 ? webPort : _defaultServer.HttpPort)}/cat/s/router?domain={Domain.Id ?? "cat"}&ip={AppEnv.IP}" : null;
        }
    }
}