using PureCat.Configuration;
using PureCat.Context;
using PureCat.Message;
using PureCat.Message.Spi;
using PureCat.Message.Spi.Internals;
using PureCat.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace PureCat
{
    public class PureCat
    {
        private static readonly string _configPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "App_Data\\TCConfig\\CatConfig.xml");
        private static readonly PureCat _instance = null;
        private static readonly object _lock = new object();

        private bool _mInitialized;

        private IMessageManager _mManager;

        private IMessageProducer _mProducer;

        static PureCat()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new PureCat();
                    }
                }
            }
        }

        public static IMessageManager GetManager()
        {
            return _instance._mManager;
        }

        public static IMessageProducer GetProducer()
        {
            return _instance._mProducer;
        }

        public static void Initialize(ClientConfig clientConfig)
        {
            Logger.Info("Cat.Version : {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            if (_instance._mInitialized)
                return;

            clientConfig = clientConfig ?? LoadClientConfig(_configPath);

            Logger.Info("Initializing Cat .Net Client ...");

            DefaultMessageManager manager = new DefaultMessageManager();

            manager.InitializeClient(clientConfig);
            _instance._mProducer = new DefaultMessageProducer(manager);
            _instance._mManager = manager;
            _instance._mInitialized = true;
            Logger.Info("Cat .Net Client initialized.");
        }

        public static void Initialize(string configFile = null)
        {
            ClientConfig clientConfig = LoadClientConfig(string.IsNullOrWhiteSpace(configFile) ? _configPath : configFile);

            Initialize(clientConfig);
        }

        public static bool IsInitialized()
        {
            bool isInitialized = _instance._mInitialized;
            if (isInitialized && !_instance._mManager.HasContext())
            {
                _instance._mManager.Setup();
            }
            return isInitialized;
        }

        public static ITransaction NewTransaction(string type, string name)
        {
            return GetProducer().NewTransaction(type, name);
        }

        public static IEvent NewEvent(string type, string name)
        {
            return GetProducer().NewEvent(type, name);
        }

        public static ITrace NewTrace(string type, string name)
        {
            return GetProducer().NewTrace(type, name);
        }

        public static string GetCurrentMessageId()
        {
            var tree = GetManager().ThreadLocalMessageTree;
            if (tree != null)
            {
                if (tree.MessageId == null)
                {
                    tree.MessageId = CreateMessageId();
                }
                return tree.MessageId;
            }
            else
            {
                return null;
            }
        }

        public static string CreateMessageId()
        {
            return GetProducer().CreateMessageId();
        }



        public static CatContext LogRemoteCallClient(string contextName)
        {
            var ctx = new CatContext(contextName);

            var tree = GetManager().ThreadLocalMessageTree;

            if (tree.MessageId == null)
            {
                tree.MessageId = CreateMessageId();
            }

            var messageId = tree.MessageId;

            var childId = CreateMessageId();
            LogEvent("RemoteCall", ctx.ContextName, "0", childId);

            var rootId = tree.RootMessageId;

            if (rootId == null)
            {
                rootId = tree.MessageId;
            }

            ctx.CatRootId = rootId;
            ctx.CatParentId = messageId;
            ctx.CatChildId = childId;

            return ctx;
        }

        public static void LogRemoteCallServer(CatContext ctx)
        {
            if (ctx == null)
            {
                return;
            }

            var tree = GetManager().ThreadLocalMessageTree;
            var messageId = ctx.CatChildId;
            var rootId = ctx.CatRootId;
            var parentId = ctx.CatParentId;

            if (messageId != null)
            {
                tree.MessageId = messageId;
            }
            if (parentId != null)
            {
                tree.ParentMessageId = parentId;
            }
            if (rootId != null)
            {
                tree.RootMessageId = rootId;
            }
        }


        public static void LogEvent(string type, string name, string status = "0", string nameValuePairs = null)
        {
            GetProducer().LogEvent(type, name, status, nameValuePairs);
        }

        public static void LogHeartbeat(string type, string name, string status = "0", string nameValuePairs = null)
        {
            GetProducer().LogHeartbeat(type, name, status, nameValuePairs);
        }

        public static void LogError(Exception ex)
        {
            GetProducer().LogError(ex);
        }


        public static void LogMetricForCount(string name, int count = 1)
        {
            LogMetricInternal(name, "C", count.ToString());
        }

        public static void LogMetricForDuration(string name, double value)
        {
            LogMetricInternal(name, "T", string.Format("{0:F}", value));
        }

        public static void logMetricForSum(string name, double value)
        {
            LogMetricInternal(name, "S", string.Format("{0:F}", value));
        }

        public static void LogMetricForSum(string name, double value, int count = 1)
        {
            LogMetricInternal(name, "S,C", string.Format("{0},{1:F}", count, value));
        }





        private static void LogMetricInternal(string name, string status, string keyValuePairs = null)
        {
            GetProducer().LogMetric(name, status, keyValuePairs);
        }
        #region 配置文件属性获取

        private static ClientConfig LoadClientConfig(string configFile)
        {
            ClientConfig config = new ClientConfig();

            if (File.Exists(configFile))
            {
                Logger.Info("Use config file({0}).", configFile);

                XmlDocument doc = new XmlDocument();

                doc.Load(configFile);

                XmlElement root = doc.DocumentElement;

                if (root != null)
                {
                    config.Domain = BuildDomain(root.GetElementsByTagName("domain"));

                    IEnumerable<Server> servers = BuildServers(root.GetElementsByTagName("servers"));

                    //NOTE: 只添加Enabled的
                    foreach (Server server in servers.Where(server => server.Enabled))
                    {
                        config.Servers.Add(server);
                        Logger.Info("CAT server configured: {0}:{1}", server.Ip, server.Port);
                    }
                }
            }
            else
            {
                Logger.Warn("Config file({0}) not found.", configFile);
                //Logger.Warn("Config file({0}) not found, using localhost:2280 instead.", configFile);

                //config.Domain = BuildDomain(null);
                //config.Servers.Add(new Server("localhost", 2280));
            }

            return config;
        }

        private static Domain BuildDomain(XmlNodeList nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return new Domain();
            }

            XmlElement node = (XmlElement)nodes[0];
            return new Domain
            {
                Id = GetStringProperty(node, "id", "Unknown"),
                //Ip = GetStringProperty(node, "ip", null),
                Enabled = GetBooleanProperty(node, "enabled", false)
            };
        }

        private static IEnumerable<Server> BuildServers(XmlNodeList nodes)
        {
            List<Server> servers = new List<Server>();

            if (nodes != null && nodes.Count > 0)
            {
                XmlElement first = (XmlElement)nodes[0];
                XmlNodeList serverNodes = first.GetElementsByTagName("server");

                foreach (XmlNode node in serverNodes)
                {
                    XmlElement serverNode = (XmlElement)node;
                    string ip = GetStringProperty(serverNode, "ip", "localhost");
                    int port = GetIntProperty(serverNode, "port", 2280);
                    Server server = new Server(ip, port) { Enabled = GetBooleanProperty(serverNode, "enabled", true) };

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

        private static string GetStringProperty(XmlElement element, string name, string defaultValue)
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

        private static bool GetBooleanProperty(XmlElement element, string name, bool defaultValue)
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

        private static int GetIntProperty(XmlElement element, string name, int defaultValue)
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
        #endregion

    }
}