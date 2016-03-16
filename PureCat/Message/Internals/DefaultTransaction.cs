using PureCat.Message.Spi;
using PureCat.Util;
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PureCat.Message.Internals
{
    [Serializable]
    public class DefaultTransaction : AbstractMessage, ITransaction
    {
        private IList<IMessage> _mChildren;
        private long _mDurationInMicro; // must be less than 0

        private IMessageManager _messageManager;

        public DefaultTransaction(string type, string name, IMessageManager messageManager = null)
            : base(type, name, messageManager)
        {
            _messageManager = messageManager;
            _mDurationInMicro = -1;
            Standalone = true;
        }

        #region ITransaction Members

        //[JsonConverter(typeof(List<DefaultTransaction>))]
        public IList<IMessage> Children
        {
            get { return _mChildren ?? (_mChildren = new List<IMessage>()); }
        }

        public long DurationInMicros
        {
            get
            {
                if (_mDurationInMicro >= 0)
                {
                    return _mDurationInMicro;
                }
                // if it's not completed explicitly
                long duration = 0;
                int len = (_mChildren == null) ? 0 : _mChildren.Count;

                if (len > 0)
                {
                    if (_mChildren != null)
                    {
                        IMessage lastChild = _mChildren[len - 1];

                        if (lastChild is ITransaction)
                        {
                            ITransaction trx = lastChild as ITransaction;

                            duration = trx.Timestamp * 1000L + trx.DurationInMicros - TimestampInMicros;
                        }
                        else
                        {
                            duration = lastChild.Timestamp * 1000L - TimestampInMicros;
                        }
                    }
                }

                return duration;
            }
            set { _mDurationInMicro = value; }
        }

        public long DurationInMillis
        {
            get { return DurationInMicros / 1000L; }
            set { _mDurationInMicro = value * 1000L; }
        }

        public bool Standalone { get; set; }

        public ITransaction AddChild(IMessage message)
        {
            if (_mChildren == null)
            {
                _mChildren = new List<IMessage>();
            }

            if (message != null)
            {
                _mChildren.Add(message);
            }
            else
            {
                PureCatClient.LogError(new Exception("null child message"));
            }
            return this;
        }

        public override void Complete()
        {
            if (IsCompleted())
            {
                // complete() was called more than once
                IMessage @event = new DefaultEvent("CAT", "BadInstrument") { Status = "TransactionAlreadyCompleted" };

                @event.Complete();
                AddChild(@event);
            }
            else
            {
                _mDurationInMicro = MilliSecondTimer.CurrentTimeMicros() - TimestampInMicros;

                SetCompleted(true);

                if (_messageManager != null)
                {
                    _messageManager.End(this);
                }
            }
        }

        public bool HasChildren()
        {
            return _mChildren != null && _mChildren.Count > 0;
        }

        #endregion
    }
}