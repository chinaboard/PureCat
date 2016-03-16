using PureCat.Message.Spi;
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PureCat.Message.Internals
{
    [Serializable]
    public class DefaultTaggedTransaction : AbstractMessage, ITaggedTransaction
    {
        private IList<IMessage> _mChildren;
        private long _mDurationInMicro; // must be less than 0

        private string _rootMessageId;
        private string _parentMessageId;
        private string _tag;

        public string ParentMessageId { get { return _parentMessageId; } }

        public string RootMessageId { get { return _rootMessageId; } }

        public string Tag { get { return _tag; } }

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

        public DefaultTaggedTransaction(string type, string name, string tag, IMessageManager messageManager)
           : base(type, name, messageManager)
        {
            _tag = tag;
            _mDurationInMicro = -1;
            Standalone = false;

            IMessageTree tree = messageManager.ThreadLocalMessageTree;
            if (tree != null)
            {
                _rootMessageId = tree.RootMessageId;
                _parentMessageId = tree.ParentMessageId;
            }
        }

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

        public bool HasChildren()
        {
            return _mChildren != null && _mChildren.Count > 0;
        }

        public void Bind(string tag, string childMessageId, string title = null)
        {
            IEvent @event = new DefaultEvent(PureCatConstants.TYPE_REMOTE_CALL, "Tagged");

            @event.AddData(childMessageId, title ?? $"{Type}:{Name}");
            @event.Timestamp = Timestamp;
            @event.Status = PureCatConstants.SUCCESS;
            @event.Complete();

            AddChild(@event);
        }

        public void Start()
        {
            IMessageTree tree = MessageManager.ThreadLocalMessageTree;
            if (tree != null && tree.RootMessageId == null)
            {
                tree.ParentMessageId = _parentMessageId;
                tree.RootMessageId = _rootMessageId;
            }
        }


    }
}