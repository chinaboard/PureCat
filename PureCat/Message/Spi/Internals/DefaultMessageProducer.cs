using PureCat.Message.Internals;
using System;
using System.IO;

namespace PureCat.Message.Spi.Internals
{
    public class DefaultMessageProducer : IMessageProducer
    {
        private readonly IMessageManager _manager;

        public DefaultMessageProducer(IMessageManager manager)
        {
            _manager = manager;
        }

        public string CreateMessageId()
        {
            return _manager.GetMessageIdFactory().GetNextId();
        }

        #region IMessageProducer Members

        public virtual void LogError(Exception cause)
        {
            var ignore = false;
            if (cause.Data.Contains("CatIgnore") && cause.Data["CatIgnore"] is bool)
                ignore = (bool)(cause.Data["CatIgnore"]);

            if (ignore) return;

            StringWriter writer = new StringWriter();

            try
            {
                writer.WriteLine(cause.Message);
                writer.WriteLine(cause.StackTrace);
                var innerException = cause.InnerException;
                while (innerException != null)
                {
                    writer.WriteLine("-------------------------------------------------------------------");
                    writer.WriteLine(innerException.Message);
                    writer.WriteLine(innerException.StackTrace);

                    innerException = innerException.InnerException;
                }
            }
            catch
            {
            }

            LogEvent(PureCatConstants.TYPE_ERROR, cause.GetType().FullName, PureCatConstants.ERROR, writer.ToString());
        }

        public virtual void LogTrace(string type, string name, string status, string nameValuePairs)
        {
            ITrace trace = NewTrace(type, name);

            if (!string.IsNullOrEmpty(nameValuePairs))
            {
                trace.AddData(nameValuePairs);
            }

            trace.Status = status;
            trace.Complete();
        }

        public virtual void LogEvent(string type, string name, string status, string nameValuePairs)
        {
            IEvent @event = NewEvent(type, name);

            if (!string.IsNullOrEmpty(nameValuePairs))
            {
                @event.AddData(nameValuePairs);
            }

            @event.Status = status;
            @event.Complete();
        }

        public virtual void LogHeartbeat(string type, string name, string status, string nameValuePairs)
        {
            IHeartbeat heartbeat = NewHeartbeat(type, name);

            if (!string.IsNullOrEmpty(nameValuePairs))
            {
                heartbeat.AddData(nameValuePairs);
            }
            heartbeat.Status = status;
            heartbeat.Complete();
        }

        public virtual void LogMetric(string name, string status, string nameValuePairs)
        {
            string type = string.Empty;
            IMetric metric = NewMetric(type, name);

            if (!string.IsNullOrWhiteSpace(nameValuePairs))
            {
                metric.AddData(nameValuePairs);
            }

            metric.Status = status;
            metric.Complete();
        }

        public virtual IEvent NewEvent(string type, string name)
        {
            if (!_manager.HasContext())
            {
                _manager.Setup();
            }

            if (_manager.CatEnabled)
            {
                IEvent @event = new DefaultEvent(type, name);

                _manager.Add(@event);
                return @event;
            }
            return NullMessage.EVENT;
        }

        public virtual ITrace NewTrace(string type, string name)
        {
            // this enable CAT client logging cat message without explicit setup
            if (!_manager.HasContext())
            {
                _manager.Setup();
            }

            if (_manager.CatEnabled)
            {
                ITrace trace = new DefaultTrace(type, name);

                _manager.Add(trace);
                return trace;
            }
            return NullMessage.TRACE;
        }

        public virtual IMetric NewMetric(string type, string name)
        {
            // this enable CAT client logging cat message without explicit setup
            if (!_manager.HasContext())
            {
                _manager.Setup();
            }

            if (_manager.CatEnabled)
            {
                IMetric metric = new DefaultMetric(string.IsNullOrWhiteSpace(type) ? string.Empty : type, name);

                _manager.Add(metric);
                return metric;
            }
            return NullMessage.METRIC;
        }

        public virtual IHeartbeat NewHeartbeat(string type, string name)
        {
            if (!_manager.HasContext())
            {
                _manager.Setup();
            }

            if (_manager.CatEnabled)
            {
                IHeartbeat heartbeat = new DefaultHeartbeat(type, name);

                _manager.Add(heartbeat);
                return heartbeat;
            }
            return NullMessage.HEARTBEAT;
        }

        public virtual ITransaction NewTransaction(string type, string name)
        {
            // this enable CAT client logging cat message without explicit setup
            if (!_manager.HasContext())
            {
                _manager.Setup();
            }

            if (_manager.CatEnabled)
            {
                ITransaction transaction = new DefaultTransaction(type, name, _manager);

                _manager.Start(transaction, false);
                return transaction;
            }
            return NullMessage.TRANSACTION;
        }

        public virtual ITransaction NewTransaction(ITransaction parent, string type, string name)
        {
            // this enable CAT client logging cat message without explicit setup
            if (!_manager.HasContext())
            {
                _manager.Setup();
            }

            if (_manager.CatEnabled && parent != null)
            {
                ITransaction transaction = new DefaultTransaction(type, name, _manager);

                parent.AddChild(transaction);
                transaction.Standalone = false;
                return transaction;
            }
            return NullMessage.TRANSACTION;
        }



        public IForkedTransaction NewForkedTransaction(string type, string name)
        {
            // this enable CAT client logging cat message without explicit setup
            if (!_manager.HasContext())
            {
                _manager.Setup();
            }

            if (_manager.CatEnabled)
            {
                IMessageTree tree = _manager.ThreadLocalMessageTree;

                if (tree.MessageId == null)
                {
                    tree.MessageId = CreateMessageId();
                }

                IForkedTransaction transaction = new DefaultForkedTransaction(type, name, _manager);

                if (_manager is DefaultMessageManager)
                {
                    ((DefaultMessageManager)_manager).LinkAsRunAway(transaction);
                }
                _manager.Start(transaction, true);
                return transaction;
            }
            else
            {
                return NullMessage.FORKEDTRANSACTION;
            }
        }

        public ITaggedTransaction NewTaggedTransaction(string type, string name, string tag)
        {
            // this enable CAT client logging cat message without explicit setup
            if (!_manager.HasContext())
            {
                _manager.Setup();
            }

            if (_manager.CatEnabled)
            {
                IMessageTree tree = _manager.ThreadLocalMessageTree;

                if (tree.MessageId == null)
                {
                    tree.MessageId = CreateMessageId();
                }

                ITaggedTransaction transaction = new DefaultTaggedTransaction(type, name, tag, _manager);

                _manager.Start(transaction, true);
                return transaction;
            }
            else
            {
                return NullMessage.TAGGEDTRANSACTION;
            }
        }

        #endregion

    }
}