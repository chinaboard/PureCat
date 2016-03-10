using PureCat.Configuration;
using PureCat.Context;
using PureCat.Message;
using PureCat.Message.Spi;
using PureCat.Message.Spi.Internals;
using PureCat.Util;
using System;
using System.IO;

namespace PureCat
{
    public class PureCat
    {

        #region Other
        private static readonly PureCat _instance = null;
        private static readonly object _lock = new object();

        public static string Version { get { return $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}"; } }

        public bool Initialized { get; private set; } = false;

        public IMessageManager MessageManager { get; private set; }

        public IMessageProducer MessageProducer { get; private set; }

        public static IMessageManager GetManager()
        {
            return _instance.MessageManager;
        }

        public static IMessageProducer GetProducer()
        {
            return _instance.MessageProducer;
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

        public static bool IsInitialized()
        {
            bool isInitialized = _instance.Initialized;
            if (isInitialized && !_instance.MessageManager.HasContext())
            {
                _instance.MessageManager.Setup();
            }
            return isInitialized;
        }
        #endregion

        #region Initialize

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

        /// <summary>
        /// 根据配置文件初始化PureCat，默认使用CatConfig.xml
        /// </summary>
        /// <param name="configFilePath">配置文件路径</param>
        public static void Initialize(string configFilePath = null)
        {
            Initialize(new ClientConfigManager(configFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CatConfig.xml")));
        }

        /// <summary>
        /// 根据ClientConfig初始化PureCat
        /// </summary>
        public static void Initialize(ClientConfig clientConfig)
        {
            Initialize(new ClientConfigManager(clientConfig));
        }

        private static void Initialize(ClientConfigManager configManager)
        {
            if (_instance.Initialized)
                return;
            Logger.Info($"Initializing Cat .Net Client ...Cat.Version : {PureCat.Version}");
            DefaultMessageManager manager = new DefaultMessageManager();

            manager.InitializeClient(configManager.ClientConfig);
            _instance.MessageManager = manager;
            _instance.MessageProducer = new DefaultMessageProducer(manager);
            _instance.Initialized = true;
            Logger.Info("Cat .Net Client initialized.");
        }

        #endregion

        #region DoTransaction
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <param name="customCatch">捕获异常时的处理方法</param>
        public static T DoTransaction<T>(string type, string name, Func<T> func, Func<Exception, T> customCatch = null)
        {
            return DoTransaction<Exception, T>(type, name, func, customCatch);
        }
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <param name="customCatch">捕获异常时的处理方法</param>
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
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <param name="customCatch">捕获异常时的处理方法</param>
        public static void DoTransaction(string type, string name, Action action, Action<Exception> customCatch = null)
        {
            DoTransaction<Exception>(type, name, action, customCatch);
        }
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <param name="customCatch">捕获异常时的处理方法</param>
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
        #endregion

        #region LogView
        /// <summary>
        /// 客户端创建请求上下文
        /// </summary>
        /// <param name="contextName">上下文名称</param>
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

        /// <summary>
        /// 服务端串联上下文
        /// </summary>
        /// <param name="context">上下文</param>
        public static void LogRemoteCallServer(CatContext context)
        {
            if (context == null)
            {
                return;
            }

            var tree = GetManager().ThreadLocalMessageTree;
            var messageId = context.CatChildId;
            var rootId = context.CatRootId;
            var parentId = context.CatParentId;

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
        #endregion

        #region LogEvent
        public static void LogEvent(string type, string name, string status = PureCatConstants.SUCCESS, string nameValuePairs = null)
        {
            GetProducer().LogEvent(type, name, status, nameValuePairs);
        }

        public static void LogTrace(string type, string name, string status = PureCatConstants.SUCCESS, string nameValuePairs = null)
        {
            GetProducer().LogTrace(type, name, status, nameValuePairs);
        }

        public static void LogHeartbeat(string type, string name, string status = PureCatConstants.SUCCESS, string nameValuePairs = null)
        {
            GetProducer().LogHeartbeat(type, name, status, nameValuePairs);
        }

        public static void LogError(Exception ex)
        {
            GetProducer().LogError(ex);
        }
        #endregion

        #region New

        public static IEvent NewEvent(string type, string name)
        {
            return GetProducer().NewEvent(type, name);
        }

        public static ITrace NewTrace(string type, string name)
        {
            return GetProducer().NewTrace(type, name);
        }

        public static IHeartbeat NewHeartbeat(string type, string name)
        {
            return GetProducer().NewHeartbeat(type, name);
        }

        public static ITransaction NewTransaction(string type, string name)
        {
            return GetProducer().NewTransaction(type, name);
        }

        public static ITaggedTransaction NewTaggedTransaction(string type, string name, string tag)
        {
            return GetProducer().NewTaggedTransaction(type, name, tag);
        }

        public static IForkedTransaction NewForkedTransaction(string type, string name)
        {
            return GetProducer().NewForkedTransaction(type, name);
        }

        #endregion

        #region Metric

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

        #endregion

    }
}