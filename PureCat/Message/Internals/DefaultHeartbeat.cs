using PureCat.Message.Spi;

namespace PureCat.Message.Internals
{
    public class DefaultHeartbeat : AbstractMessage, IHeartbeat
    {
        private readonly IMessageManager _messageManager;
        public DefaultHeartbeat(string type, string name, IMessageManager messageManager = null)
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