using PureCat.Message.Spi;

namespace PureCat.Message.Internals
{
    public class DefaultTrace : AbstractMessage, ITrace
    {
        private readonly IMessageManager _messageManager;
        public DefaultTrace(string type, string name, IMessageManager messageManager = null)
            : base(type, name, messageManager)
        {
            _messageManager = messageManager;
        }
        public override void Complete()
        {
            SetCompleted(true);

            if (_messageManager != null)
            {
                _messageManager.Add(this);
            }
        }
    }
}
