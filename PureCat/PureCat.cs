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

            Logger.Info("Initializing Cat .Net Client ...");

            DefaultMessageManager manager = new DefaultMessageManager();

            manager.InitializeClient(clientConfig);
            _instance._mProducer = new DefaultMessageProducer(manager);
            _instance._mManager = manager;
            _instance._mInitialized = true;
            Logger.Info("Cat .Net Client initialized.");
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

    }
}