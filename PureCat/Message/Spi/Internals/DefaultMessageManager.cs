using System.Globalization;
using PureCat.Message.Internals;
using PureCat.Message.Spi.IO;
using PureCat.Util;
using PureCat.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;

namespace PureCat.Message.Spi.Internals
{
    [Serializable]
    public class DefaultMessageManager : IMessageManager
    {
        // we don't use static modifier since MessageManager is a singleton in
        // production actually
        private readonly CatThreadLocal<Context> _mContext = new CatThreadLocal<Context>();

        private ClientConfig _mClientConfig;

        private MessageIdFactory _mFactory;

        private bool _mFirstMessage = true;

        private string _mHostName;

        private IMessageSender _mSender;

        private IMessageStatistics _mStatistics;

        private StatusUpdateTask _mStatusUpdateTask;

        private ConcurrentDictionary<string, ITaggedTransaction> _taggedTransactions;

        #region IMessageManager Members

        public virtual ClientConfig ClientConfig
        {
            get { return _mClientConfig; }
        }

        public virtual ITransaction PeekTransaction
        {
            get
            {
                Context ctx = GetContext();

                if (ctx == null)
                {
                    Setup();
                }

                ctx = _mContext.Value;

                return ctx.PeekTransaction();
            }
        }

        public virtual IMessageTree ThreadLocalMessageTree
        {
            get
            {
                Context ctx = _mContext.Value;

                if (ctx == null)
                {
                    Setup();
                }

                ctx = _mContext.Value;

                return ctx.Tree;
            }
        }

        public virtual void Reset()
        {
            // destroy current thread local data
            _mContext.Dispose();
        }

        public virtual void InitializeClient(ClientConfig clientConfig)
        {
            _mClientConfig = clientConfig ?? new ClientConfig();

            _mHostName = NetworkInterfaceManager.GetLocalHostName();
            _mStatistics = new DefaultMessageStatistics();
            _mSender = new TcpMessageSender(_mClientConfig, _mStatistics);
            _mSender.Initialize();
            _mFactory = new MessageIdFactory();
            _mStatusUpdateTask = new StatusUpdateTask(_mStatistics);

            _taggedTransactions = new ConcurrentDictionary<string, ITaggedTransaction>();

            // initialize domain and ip address
            _mFactory.Initialize(_mClientConfig.Domain.Id);

            // start status update task
            ThreadPool.QueueUserWorkItem(_mStatusUpdateTask.Run);
            Logger.Info("Thread(StatusUpdateTask) started.");
        }

        public virtual bool HasContext()
        {
            return _mContext.Value != null;
        }

        public virtual bool CatEnabled
        {
            get { return _mClientConfig.Domain.Enabled && _mContext.Value != null; }
        }

        public virtual void Add(IMessage message)
        {
            Context ctx = GetContext();

            if (ctx != null)
            {
                ctx.Add(this, message);
            }
        }

        public void Bind(string tag, string title)
        {
            ITaggedTransaction t = null;

            if (_taggedTransactions.TryGetValue(tag, out t))
            {
                IMessageTree tree = ThreadLocalMessageTree;

                if (tree != null)
                {
                    if (tree.MessageId == null)
                    {
                        tree.MessageId = NextMessageId();
                    }

                    t.Start();
                    t.Bind(tag, tree.MessageId, title);
                }
            }
        }

        public virtual void Setup()
        {
            Context ctx = new Context(_mClientConfig.Domain.Id, _mHostName,
                                      NetworkInterfaceManager.GetLocalHostAddress());

            _mContext.Value = ctx;
        }

        public virtual void Start(ITransaction transaction, bool forked)
        {
            Context ctx = GetContext();

            if (ctx != null)
            {
                ctx.Start(this, transaction, forked);

                if (transaction is DefaultTaggedTransaction)
                {
                    ITaggedTransaction tt = transaction as DefaultTaggedTransaction;
                    _taggedTransactions[tt.Tag] = tt;
                }
            }
            else if (_mFirstMessage)
            {
                _mFirstMessage = false;
                Logger.Warn("CAT client is not enabled because it's not initialized yet");
            }
        }

        public virtual void End(ITransaction transaction)
        {
            Context ctx = GetContext();

            if (ctx != null)
            {
                //if (!transaction.Standalone) return;
                if (ctx.End(this, transaction))
                {
                    _mContext.Dispose();
                }
            }
            else
                Logger.Warn("Context没取到");
        }

        #endregion

        public MessageIdFactory GetMessageIdFactory()
        {
            return _mFactory;
        }

        internal void Flush(IMessageTree tree)
        {
            if (_mSender != null)
            {
                _mSender.Send(tree);

                if (_mStatistics != null)
                {
                    _mStatistics.OnSending(tree);
                }
            }
        }

        internal Context GetContext()
        {
            if (PureCat.IsInitialized())
            {
                Context ctx = _mContext.Value;

                if (ctx != null)
                {
                    return ctx;
                }
            }

            return null;
        }

        public void LinkAsRunAway(IForkedTransaction transaction)
        {
            Context ctx = GetContext();
            if (ctx != null)
            {
                ctx.LinkAsRunAway(this, transaction);
            }
        }

        public string NextMessageId()
        {
            return _mFactory.GetNextId();
        }

        #region Nested type: Context

        internal class Context
        {
            private readonly Stack<ITransaction> _mStack;
            private readonly IMessageTree _mTree;

            public Context(string domain, string hostName, string ipAddress)
            {
                _mTree = new DefaultMessageTree();
                _mStack = new Stack<ITransaction>();

                Thread thread = Thread.CurrentThread;
                string groupName = Thread.GetDomain().FriendlyName;

                _mTree.ThreadGroupName = groupName;
                _mTree.ThreadId = thread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
                _mTree.ThreadName = thread.Name;

                _mTree.Domain = domain;
                _mTree.HostName = hostName;
                _mTree.IpAddress = ipAddress;
            }

            public IMessageTree Tree
            {
                get { return _mTree; }
            }

            /// <summary>
            ///   添加Event和Heartbeat
            /// </summary>
            /// <param name="manager"> </param>
            /// <param name="message"> </param>
            public void Add(DefaultMessageManager manager, IMessage message)
            {
                if ((_mStack.Count == 0))
                {
                    IMessageTree tree = _mTree.Copy();
                    tree.MessageId = manager.NextMessageId();
                    tree.Message = message;
                    manager.Flush(tree);
                }
                else
                {
                    ITransaction entry = _mStack.Peek();
                    entry.AddChild(message);
                }
            }

            ///<summary>
            ///  return true means the transaction has been flushed.
            ///</summary>
            ///<param name="manager"> </param>
            ///<param name="transaction"> </param>
            ///<returns> true if message is flushed, false otherwise </returns>
            public bool End(DefaultMessageManager manager, ITransaction transaction)
            {
                try
                {
                    if (_mStack.Count != 0)
                    {
                        ITransaction current = _mStack.Pop();

                        if (transaction == current)
                        {
                            ValidateTransaction(manager, _mStack.Count == 0 ? null : _mStack.Peek(), current);
                        }
                        else
                        {
                            while (transaction != current && _mStack.Count != 0)
                            {
                                current = _mStack.Pop();
                            }
                        }


                        if (_mStack.Count == 0)
                        {
                            IMessageTree tree = _mTree.Copy();
                            _mTree.MessageId = null;
                            _mTree.Message = null;

                            manager.Flush(tree);
                            return true;
                        }
                        return false;
                    }
                    throw new Exception("Stack为空, 没找到对应的Transaction.");

                }
                catch (Exception ex)
                {
                    var exTran = PureCat.GetProducer().NewTransaction("Cat", "CatMessageManager");
                    PureCat.GetProducer().LogError(ex);
                    exTran.SetStatus(ex);
                    exTran.Complete();
                    return false;
                }
            }

            /// <summary>
            ///   返回stack的顶部对象
            /// </summary>
            /// <returns> </returns>
            public ITransaction PeekTransaction()
            {
                return (_mStack.Count == 0) ? null : _mStack.Peek();
            }

            /// <summary>
            ///   添加transaction
            /// </summary>
            /// <param name="manager"> </param>
            /// <param name="transaction"> </param>
            public void Start(DefaultMessageManager manager, ITransaction transaction, bool forked)
            {
                if (_mStack.Count != 0)
                {
                    if (!(transaction is DefaultForkedTransaction))
                    {
                        ITransaction parent = _mStack.Peek();
                        AddTransactionChild(manager, transaction, parent);
                    }
                }
                else
                {
                    _mTree.MessageId = manager.NextMessageId();
                    _mTree.Message = transaction;
                }

                if (!forked)
                {
                    _mStack.Push(transaction);
                }
            }

            internal void LinkAsRunAway(DefaultMessageManager manager, IForkedTransaction transaction)
            {
                IEvent @event = new DefaultEvent(PureCatConstants.TYPE_REMOTE_CALL, "RunAway");

                @event.AddData(transaction.ForkedMessageId, $"{transaction.Type}:{transaction.Name}");
                @event.Timestamp = transaction.Timestamp;
                @event.Status = PureCatConstants.SUCCESS;
                @event.Complete();

                transaction.Standalone = true;

                manager.Add(@event);
            }
            private void MarkAsRunAway(ITransaction parent, DefaultTaggedTransaction transaction)
            {
                if (!transaction.HasChildren())
                {
                    transaction.AddData("RunAway");
                }

                transaction.Status = PureCatConstants.SUCCESS;
                transaction.Standalone = true;
                transaction.Complete();
            }

            private void MarkAsNotCompleted(DefaultTransaction transaction)
            {
                IEvent notCompleteEvent = new DefaultEvent("CAT", "BadInstrument") { Status = "TransactionNotCompleted" };
                notCompleteEvent.Complete();
                transaction.AddChild(notCompleteEvent);
                transaction.Complete();
            }


            //验证Transaction
            internal void ValidateTransaction(DefaultMessageManager manager, ITransaction parent, ITransaction transaction)
            {
                if (transaction.Standalone)
                {
                    IList<IMessage> children = transaction.Children;
                    int len = children.Count;
                    for (int i = 0; i < len; i++)
                    {
                        IMessage message = children[i];

                        if (message is ITransaction)
                        {
                            ValidateTransaction(manager, transaction, message as ITransaction);
                        }
                    }

                    if (!transaction.IsCompleted() && transaction is DefaultTransaction)
                    {
                        MarkAsNotCompleted(transaction as DefaultTransaction);
                    }
                    else if (!transaction.IsCompleted())
                    {
                        if (transaction is DefaultForkedTransaction)
                        {
                            LinkAsRunAway(manager, transaction as DefaultForkedTransaction);
                        }
                        else if (transaction is DefaultTaggedTransaction)
                        {
                            MarkAsRunAway(parent, transaction as DefaultTaggedTransaction);
                        }

                    }
                }
            }

            private void AddTransactionChild(DefaultMessageManager manager, IMessage message, ITransaction transaction)
            {
                long treePeriod = TrimToHour(_mTree.Message.Timestamp);
                long messagePeriod = TrimToHour(message.Timestamp - 10 * 1000L); // 10 seconds extra time allowed

                if (treePeriod < messagePeriod)
                {
                    TruncateAndFlush(manager, message.Timestamp);
                }

                transaction.AddChild(message);
            }

            private void TruncateAndFlush(DefaultMessageManager manager, long timestamp)
            {
                IMessageTree tree = _mTree;
                Stack<ITransaction> stack = _mStack;
                IMessage message = tree.Message;

                if (message is DefaultTransaction)
                {
                    if (tree.MessageId == null)
                    {
                        tree.MessageId = manager.NextMessageId();
                    }

                    string rootId = tree.RootMessageId;
                    string childId = manager.NextMessageId();

                    DefaultTransaction source = message as DefaultTransaction;
                    DefaultTransaction target = new DefaultTransaction(source.Type, source.Name, manager);
                    target.Timestamp = source.Timestamp;
                    target.DurationInMicros = source.DurationInMicros;
                    target.AddData(source.Data);
                    target.Status = PureCatConstants.SUCCESS;

                    MigrateMessage(manager, stack, source, target, 1);

                    var list = stack.ToList();

                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        DefaultTransaction tran = list[i] as DefaultTransaction;
                        tran.Timestamp = timestamp;
                        tran.DurationInMicros = -1;
                    }

                    IEvent next = new DefaultEvent(PureCatConstants.TYPE_REMOTE_CALL, "Next");
                    next.AddData(childId);
                    next.Status = PureCatConstants.SUCCESS;
                    target.AddChild(next);

                    IMessageTree t = tree.Copy();

                    t.Message = target;

                    _mTree.MessageId = childId;
                    _mTree.ParentMessageId = tree.MessageId;
                    _mTree.RootMessageId = rootId ?? tree.MessageId;

                    manager.Flush(t);
                }
            }

            private void MigrateMessage(DefaultMessageManager manager, Stack<ITransaction> stack, ITransaction source, ITransaction target, int level)
            {
                ITransaction current = level < stack.Count ? stack.ToList()[level] : null;
                bool shouldKeep = false;

                foreach (IMessage child in source.Children)
                {
                    if (child != current)
                    {
                        target.AddChild(child);
                    }
                    else
                    {
                        DefaultTransaction cloned = new DefaultTransaction(current.Type, current.Name, manager);

                        cloned.Timestamp = current.Timestamp;
                        cloned.DurationInMicros = current.DurationInMicros;
                        cloned.AddData(current.Data);
                        cloned.Status = PureCatConstants.SUCCESS;

                        target.AddChild(cloned);
                        MigrateMessage(manager, stack, current, cloned, level + 1);

                        shouldKeep = true;
                    }
                }

                source.Children.Clear();
                if (shouldKeep)
                {
                    source.AddChild(current);
                }
            }

            private long TrimToHour(long timestamp)
            {
                return timestamp - timestamp % (3600 * 1000L);
            }
        }


    }
    #endregion
}