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
using System.Threading.Tasks;

namespace PureCat.Message.Spi.Internals
{
    public class DefaultMessageManager : IMessageManager
    {
        // we don't use static modifier since MessageManager is a singleton in
        // production actually
        private readonly CatThreadLocal<Context> _mContext = new CatThreadLocal<Context>();

        private ClientConfig _clientConfig;

        private MessageIdFactory _factory;

        private bool _firstMessage = true;

        private string _hostName;

        private IMessageSender _sender;

        private IMessageStatistics _statistics;

        private StatusUpdateTask _statusUpdateTask;

        private ConcurrentDictionary<string, ITaggedTransaction> _taggedTransactions;

        #region IMessageManager Members

        public virtual ClientConfig ClientConfig => _clientConfig;

        public virtual ITransaction PeekTransaction
        {
            get
            {
                var ctx = GetContext();

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
                var ctx = _mContext.Value;

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
            _clientConfig = clientConfig ?? new ClientConfig();

            _hostName = NetworkInterfaceManager.GetLocalHostName();
            _statistics = new DefaultMessageStatistics();
            _sender = new TcpMessageSender(_clientConfig, _statistics);
            _sender.Initialize();
            _factory = new MessageIdFactory();
            _statusUpdateTask = new StatusUpdateTask(_statistics);

            _taggedTransactions = new ConcurrentDictionary<string, ITaggedTransaction>();

            // initialize domain and ip address
            _factory.Initialize(_clientConfig.Domain.Id);

            // start status update task
#if NET40
            TaskEx.Run(_statusUpdateTask.Run);
#else
            Task.Run(_statusUpdateTask.Run);
#endif
            Logger.Info("Thread(StatusUpdateTask) started.");
        }

        public virtual bool HasContext()
        {
            return _mContext.Value != null;
        }

        public virtual bool CatEnabled => _clientConfig.Domain.Enabled && _mContext.Value != null;

        public virtual void Add(IMessage message)
        {
            var ctx = GetContext();

            ctx?.Add(this, message);
        }

        public void Bind(string tag, string title)
        {
            if (_taggedTransactions.TryGetValue(tag, out ITaggedTransaction t))
            {
                var tree = ThreadLocalMessageTree;

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
            var ctx = new Context(this, _clientConfig.Domain.Id, _hostName,
                                      NetworkInterfaceManager.GetLocalHostAddress());

            _mContext.Value = ctx;
        }

        public virtual void Start(ITransaction transaction, bool forked)
        {
            var ctx = GetContext();

            if (ctx != null)
            {
                ctx.Start(transaction, forked);

                var taggedTransaction = transaction as DefaultTaggedTransaction;
                if (taggedTransaction != null)
                {
                    _taggedTransactions[taggedTransaction.Tag] = taggedTransaction;
                }
            }
            else if (_firstMessage)
            {
                _firstMessage = false;
                Logger.Warn("CAT client is not enabled because it's not initialized yet");
            }
        }

        public virtual void End(ITransaction transaction)
        {
            var ctx = GetContext();

            if (ctx != null && transaction.Standalone)
            {
                if (ctx.End(transaction))
                {
                    _mContext.Dispose();
                }
            }
        }

        #endregion IMessageManager Members

        public MessageIdFactory GetMessageIdFactory()
        {
            return _factory;
        }

        internal void Flush(IMessageTree tree)
        {
            if (tree.MessageId == null)
            {
                tree.MessageId = NextMessageId();
            }
            if (_sender != null)
            {
                _sender.Send(tree);

                Reset();

                _statistics?.OnSending();
            }
        }

        internal Context GetContext()
        {
            if (PureCatClient.IsInitialized())
            {
                var ctx = _mContext.Value;

                if (ctx != null)
                {
                    return ctx;
                }
            }

            return null;
        }

        public void LinkAsRunAway(IForkedTransaction transaction)
        {
            var ctx = GetContext();
            ctx?.LinkAsRunAway(transaction);
        }

        public string NextMessageId()
        {
            return _factory.GetNextId();
        }

        #region Nested type: Context

        internal class Context
        {
            private readonly Stack<ITransaction> _mStack;
            private readonly IMessageTree _mTree;
            private readonly DefaultMessageManager _manager;

            public Context(DefaultMessageManager manager, string domain, string hostName, string ipAddress)
            {
                _manager = manager;
                _mTree = new DefaultMessageTree();
                _mStack = new Stack<ITransaction>();

                var thread = Thread.CurrentThread;
                var groupName = Thread.GetDomain().FriendlyName;

                _mTree.ThreadGroupName = groupName;
                _mTree.ThreadId = thread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
                _mTree.ThreadName = thread.Name;

                _mTree.Domain = domain;
                _mTree.HostName = hostName;
                _mTree.IpAddress = ipAddress;
            }

            public IMessageTree Tree => _mTree;

            /// <summary>
            ///   添加Event和Heartbeat
            /// </summary>
            /// <param name="manager"> </param>
            /// <param name="message"> </param>
            public void Add(DefaultMessageManager manager, IMessage message)
            {
                if ((_mStack.Count == 0))
                {
                    var tree = _mTree.Copy();
                    tree.MessageId = manager.NextMessageId();
                    tree.Message = message;
                    manager.Flush(tree);
                }
                else
                {
                    var entry = _mStack.Peek();
                    entry.AddChild(message);
                }
            }

            ///<summary>
            ///  return true means the transaction has been flushed.
            ///</summary>
            ///<param name="transaction"> </param>
            ///<returns> true if message is flushed, false otherwise </returns>
            public bool End(ITransaction transaction)
            {
                if (_mStack.Count != 0)
                {
                    var current = _mStack.Pop();

                    if (transaction == current)
                    {
                        ValidateTransaction(_mStack.Count == 0 ? null : _mStack.Peek(), current);
                    }
                    else
                    {
                        while (transaction != current && _mStack.Count != 0)
                        {
                            ValidateTransaction(_mStack.Peek(), current);
                            current = _mStack.Pop();
                        }
                    }

                    if (_mStack.Count == 0)
                    {
                        var tree = _mTree.Copy();
                        _mTree.MessageId = null;
                        _mTree.Message = null;

                        _manager.Flush(tree);
                        return true;
                    }
                }
                return false;
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
            /// <param name="forked"></param>
            public void Start(ITransaction transaction, bool forked)
            {
                if (_mStack.Count != 0)
                {
                    if (!(transaction is DefaultForkedTransaction))
                    {
                        ITransaction parent = _mStack.Peek();
                        AddTransactionChild(transaction, parent);
                    }
                }
                else
                {
                    _mTree.Message = transaction;
                }

                if (!forked)
                {
                    _mStack.Push(transaction);
                }
            }

            internal void LinkAsRunAway(IForkedTransaction transaction)
            {
                var @event = new DefaultEvent(PureCatConstants.TYPE_REMOTE_CALL, "RunAway");

                @event.AddData(transaction.ForkedMessageId, $"{transaction.Type}:{transaction.Name}");
                @event.Timestamp = transaction.Timestamp;
                @event.Status = PureCatConstants.SUCCESS;
                @event.Complete();

                transaction.Standalone = true;

                _manager.Add(@event);
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
                var notCompleteEvent = new DefaultEvent("CAT", "BadInstrument") { Status = "TransactionNotCompleted" };
                notCompleteEvent.Complete();
                transaction.AddChild(notCompleteEvent);
                transaction.Complete();
            }

            //验证Transaction
            internal void ValidateTransaction(ITransaction parent, ITransaction transaction)
            {
                if (transaction.Standalone)
                {
                    var children = transaction.Children;
                    var len = children.Count;
                    for (var i = 0; i < len; i++)
                    {
                        IMessage message = children[i];

                        if (message is ITransaction)
                        {
                            ValidateTransaction(transaction, message as ITransaction);
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
                            LinkAsRunAway(transaction as DefaultForkedTransaction);
                        }
                        else if (transaction is DefaultTaggedTransaction)
                        {
                            MarkAsRunAway(parent, transaction as DefaultTaggedTransaction);
                        }
                    }
                }
            }

            private void AddTransactionChild(IMessage message, ITransaction transaction)
            {
                var treePeriod = TrimToHour(_mTree.Message.Timestamp);
                var messagePeriod = TrimToHour(message.Timestamp - 10 * 1000L); // 10 seconds extra time allowed

                if (treePeriod < messagePeriod)
                {
                    TruncateAndFlush(message.Timestamp);
                }

                transaction.AddChild(message);
            }

            private void TruncateAndFlush(long timestamp)
            {
                var tree = _mTree;
                var stack = _mStack;
                var message = tree.Message;

                if (message is DefaultTransaction)
                {
                    var id = tree.MessageId ?? _manager.NextMessageId();
                    var rootId = tree.RootMessageId;
                    var childId = _manager.NextMessageId();

                    var source = message as DefaultTransaction;
                    var target = new DefaultTransaction(source.Type, source.Name, _manager);
                    target.Timestamp = source.Timestamp;
                    target.DurationInMicros = source.DurationInMicros;
                    target.AddData(source.Data);
                    target.Status = PureCatConstants.SUCCESS;

                    MigrateMessage(stack, source, target, 1);

                    var list = stack.ToList();

                    for (var i = list.Count - 1; i >= 0; i--)
                    {
                        var tran = list[i] as DefaultTransaction;
                        if (tran != null)
                        {
                            tran.Timestamp = timestamp;
                            tran.DurationInMicros = -1;
                        }
                    }

                    var next = new DefaultEvent(PureCatConstants.TYPE_REMOTE_CALL, "Next");
                    next.AddData(childId);
                    next.Status = PureCatConstants.SUCCESS;
                    target.AddChild(next);

                    var t = tree.Copy();

                    t.Message = target;

                    _mTree.MessageId = childId;
                    _mTree.ParentMessageId = id;
                    _mTree.RootMessageId = rootId ?? tree.MessageId;

                    _manager.Flush(t);
                }
            }

            private void MigrateMessage(Stack<ITransaction> stack, ITransaction source, ITransaction target, int level)
            {
                var current = level < stack.Count ? stack.ToList()[level] : null;
                var shouldKeep = false;

                foreach (IMessage child in source.Children)
                {
                    if (child != current)
                    {
                        target.AddChild(child);
                    }
                    else
                    {
                        DefaultTransaction cloned = new DefaultTransaction(current.Type, current.Name, _manager);

                        cloned.Timestamp = current.Timestamp;
                        cloned.DurationInMicros = current.DurationInMicros;
                        cloned.AddData(current.Data);
                        cloned.Status = PureCatConstants.SUCCESS;

                        target.AddChild(cloned);
                        MigrateMessage(stack, current, cloned, level + 1);

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

    #endregion Nested type: Context
}