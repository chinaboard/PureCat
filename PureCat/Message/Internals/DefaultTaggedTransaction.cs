using PureCat.Message.Spi;
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PureCat.Message.Internals
{

    public class DefaultTaggedTransaction : DefaultTransaction, ITaggedTransaction
    {
        private IList<IMessage> _mChildren;
        private long _mDurationInMicro; // must be less than 0

        private string _rootMessageId;
        private string _parentMessageId;
        private string _tag;

        public string ParentMessageId { get { return _parentMessageId; } }

        public string RootMessageId { get { return _rootMessageId; } }

        public string Tag { get { return _tag; } }


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