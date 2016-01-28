using PureCat.Message.Spi;
using PureCat.Util;
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PureCat.Message.Internals
{
    [Serializable]
    public class DefaultForkedTransaction : DefaultTransaction, IForkedTransaction
    {
        private string _rootMessageId;

        private string _parentMessageId;

        private string _forkedMessageId;

        public DefaultForkedTransaction(string type, string name, IMessageManager messageManager)
            : base(type, name, messageManager)
        {

            Standalone = false;

            IMessageTree tree = messageManager.ThreadLocalMessageTree;

            if (tree != null)
            {
                _rootMessageId = tree.RootMessageId;
                _parentMessageId = tree.MessageId;

                // Detach parent transaction and this forked transaction, by calling linkAsRunAway(), at this earliest moment,
                // so that thread synchronization is not needed at all between them in the future.
                _forkedMessageId = PureCat.CreateMessageId();
            }
        }

        public string ForkedMessageId { get { return _forkedMessageId; } }

        public void Fork()
        {
            IMessageManager manager = MessageManager;
            manager.Setup();
            manager.Start(this, false);

            IMessageTree tree = manager.ThreadLocalMessageTree;

            if (tree != null)
            {
                tree.MessageId = _forkedMessageId;
                tree.RootMessageId = _rootMessageId ?? _parentMessageId;
                tree.ParentMessageId = _parentMessageId;
            }
        }
    }
}