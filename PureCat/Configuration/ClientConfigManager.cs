using PureCat.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace PureCat.Configuration
{
    internal class ClientConfigManager
    {
        public ClientConfig ClientConfig { get; private set; }

        public ClientConfigManager(ClientConfig clientConfig)
        {
            Initialize(clientConfig);
        }
        public ClientConfigManager(string configPath)
        {
            Initialize(configPath);
        }
        public ClientConfigManager(XmlDocument configXml)
        {
            Initialize(configXml);
        }


        private void Initialize(ClientConfig clientConfig)
        {
            if (clientConfig != null)
            {
                ClientConfig = clientConfig;
                LoadServerConfig();
                ClientConfig.RandomServer();
            }
            else
            {
                Logger.Warn($"ClientConfig is null.");
            }
        }
        private void Initialize(string configPath)
        {
            if (File.Exists(configPath))
            {
                var configXml = new XmlDocument();
                configXml.Load(configPath);
                Initialize(configXml);
            }
            else
            {
                Logger.Warn($"Config file({configPath}) not found.");
            }
        }
        private void Initialize(XmlDocument configXml)
        {
            var config = new ClientConfig();
            if (configXml != null)
            {
                var root = configXml.DocumentElement;

                if (root != null)
                {
                    var domain = BuildDomain(root.GetElementsByTagName("domain"));
                    var servers = BuildServers(root.GetElementsByTagName("servers")).Where(server => server.Enabled).ToList();

                    config.Domain = domain;
                    servers.ForEach(server =>
                    {
                        config.Servers.Add(server);
                        Logger.Info("Cat server configured: {0}:{1}", server.Ip, server.Port);
                    });
                }
            }
            else
            {
                Logger.Warn($"configXml is null.");
            }
            Initialize(config);
        }


        private Domain BuildDomain(XmlNodeList nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return new Domain();
            }

            XmlElement node = (XmlElement)nodes[0];
            return new Domain(GetStringProperty(node, "id", "Unknown"), GetBooleanProperty(node, "enabled", false), GetIntProperty(node, "queuesize", 100000));
        }

        private IEnumerable<Server> BuildServers(XmlNodeList nodes)
        {
            List<Server> servers = new List<Server>();

            if (nodes != null && nodes.Count > 0)
            {
                XmlElement first = (XmlElement)nodes[0];
                XmlNodeList serverNodes = first.GetElementsByTagName("server");

                foreach (XmlNode node in serverNodes)
                {
                    XmlElement serverNode = (XmlElement)node;
                    var ip = GetStringProperty(serverNode, "ip", "localhost");
                    var port = GetIntProperty(serverNode, "port", 2280);
                    var webport = GetIntProperty(serverNode, "webport", 8080);
                    var server = new Server(ip, port) { Enabled = GetBooleanProperty(serverNode, "enabled", true) };

                    servers.Add(server);
                }
            }

            if (servers.Count == 0)
            {
                Logger.Warn("No server configured, use localhost:2280 instead.");
                servers.Add(new Server("localhost", 2280));
            }

            return servers;
        }

        private string GetStringProperty(XmlElement element, string name, string defaultValue)
        {
            if (element != null)
            {
                string value = element.GetAttribute(name);

                if (value.Length > 0)
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private bool GetBooleanProperty(XmlElement element, string name, bool defaultValue)
        {
            if (element != null)
            {
                string value = element.GetAttribute(name);

                if (value.Length > 0)
                {
                    return "true".Equals(value);
                }
            }

            return defaultValue;
        }

        private int GetIntProperty(XmlElement element, string name, int defaultValue)
        {
            if (element != null)
            {
                string value = element.GetAttribute(name);

                if (value.Length > 0)
                {
                    int tmpRet;
                    if (int.TryParse(value, out tmpRet))
                        return tmpRet;
                }
            }

            return defaultValue;
        }

        private string GetServerConfigUrl(int webPort = -1)
        {
            if (ClientConfig == null)
                return null;
            var serverList = ClientConfig.Servers.Where(server => server.Enabled);
            foreach (var server in serverList)
            {
                return $"http://{server.Ip}:{(webPort > 0 ? webPort : server.WebPort)}/cat/s/router?domain={ClientConfig.Domain.Id}";
            }
            return null;
        }

        private void LoadServerConfig()
        {
            var serverListContent = CatHttpRequest.GetRequest(GetServerConfigUrl());
            if (string.IsNullOrWhiteSpace(serverListContent))
            {
                serverListContent = CatHttpRequest.GetRequest(GetServerConfigUrl(9005));
            }
            if (string.IsNullOrWhiteSpace(serverListContent))
            {
                return;
            }

            Logger.Info($"Get servers : {serverListContent}");


            var serverListSplit = serverListContent.TrimEnd(';').Split(';');

            List<Server> serverList = new List<Server>();

            foreach (var serverContent in serverListSplit)
            {
                try
                {
                    var content = serverContent.Split(':');
                    var ip = content[0];
                    var port = content[1];
                    serverList.Add(new Server(ip, int.Parse(port)));
                }
                catch
                {
                }
            }

            if (serverList.Count > 0)
            {
                ClientConfig.Servers = serverList;
            }
        }
    }
}
