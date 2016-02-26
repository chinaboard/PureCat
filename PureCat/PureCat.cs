using PureCat.Configuration;
using PureCat.Context;
using PureCat.Message;
using PureCat.Message.Spi;
using PureCat.Message.Spi.Internals;
using PureCat.Util;
using System;

namespace PureCat
{
    public class PureCat
    {
        private static readonly PureCat _instance = null;
        private static readonly object _lock = new object();

        public bool Initialized { get; private set; } = false;

        public IMessageManager MessageManager { get; private set; }

        public IMessageProducer MessageProducer { get; private set; }

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

        public static void Initialize()
        {
            if (_instance.Initialized)
                return;
            Logger.Info($"Initializing Cat .Net Client ...Cat.Version : {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");

            ClientConfigManager configManager = new ClientConfigManager("CatConfig.xml");
            DefaultMessageManager manager = new DefaultMessageManager();

            manager.InitializeClient(configManager.ClientConfig);
            _instance.MessageManager = manager;
            _instance.MessageProducer = new DefaultMessageProducer(manager);
            _instance.Initialized = true;
            Logger.Info("Cat .Net Client initialized.");
        }

        public static IMessageManager GetManager()
        {
            return _instance.MessageManager;
        }

        public static IMessageProducer GetProducer()
        {
            return _instance.MessageProducer;
        }

        public static bool IsInitialized()
        {
            bool isInitialized = _instance.Initialized;
            if (isInitialized && !_instance.MessageManager.HasContext())
            {
                _instance.MessageManager.Setup();
            }
            return isInitialized;
        }


        public static T DoTransaction<T>(string type, string name, Func<T> func, Func<Exception, T> customCatch = null)
        {
            return DoTransaction<Exception, T>(type, name, func, customCatch);
        }

        public static T DoTransaction<Ex, T>(string type, string name, Func<T> func, Func<Ex, T> customCatch = null) where Ex : Exception
        {
            var tran = NewTransaction(type, name);
            try
            {
                return func();
            }
            catch (Ex ex)
            {
                LogError(ex);
                tran.SetStatus(ex);
                if (customCatch != null)
                {
                    return customCatch(ex);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                tran.Complete();
            }
        }

        public static void DoTransaction(string type, string name, Action action, Action<Exception> customCatch = null)
        {
            DoTransaction<Exception>(type, name, action, customCatch);
        }

        public static void DoTransaction<Ex>(string type, string name, Action action, Action<Ex> customCatch = null) where Ex : Exception
        {
            var tran = NewTransaction(type, name);
            try
            {
                action();
            }
            catch (Ex ex)
            {
                LogError(ex);
                tran.SetStatus(ex);
                if (customCatch != null)
                {
                    customCatch(ex);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                tran.Complete();
            }
        }


        public static ITaggedTransaction NewTaggedTransaction(string type, string name, string tag)
        {
            return GetProducer().NewTaggedTransaction(type, name, tag);
        }

        public static IForkedTransaction NewForkedTransaction(string type, string name)
        {
            return GetProducer().NewForkedTransaction(type, name);
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
            LogEvent(PureCatConstants.TYPE_REMOTE_CALL, ctx.ContextName, PureCatConstants.SUCCESS, childId);

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


        public static void LogEvent(string type, string name, string status = PureCatConstants.SUCCESS, string nameValuePairs = null)
        {
            GetProducer().LogEvent(type, name, status, nameValuePairs);
        }

        public static void LogHeartbeat(string type, string name, string status = PureCatConstants.SUCCESS, string nameValuePairs = null)
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

        public static void LogMetricForSum(string name, double value)
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