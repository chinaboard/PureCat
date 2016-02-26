using PureCat.Message.Spi;

namespace PureCat.Message.Internals
{
    public class DefaultMetric : AbstractMessage, IMetric
    {
        private readonly IMessageManager _messageManager;
        public DefaultMetric(string type, string name, IMessageManager messageManager = null)
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
