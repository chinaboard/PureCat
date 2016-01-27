using System.Xml.Serialization;

namespace PureCat.Message.Spi.Heartbeat
{
    [XmlRoot("message")]
    public class MessageInfo : IRefresh
    {
        private IMessageStatistics _statistics;

        public MessageInfo()
        {
        }

        public MessageInfo(IMessageStatistics statistics)
        {
            _statistics = statistics;
        }

        [XmlAttribute("produced")]
        public long Produced { get; set; }

        [XmlAttribute("overflowed")]
        public long Overflowed { get; set; }

        [XmlAttribute("bytes")]
        public long Bytes { get; set; }

        public void Refresh()
        {
            Produced = _statistics.Produced;
            Overflowed = _statistics.Overflowed;
            Bytes = _statistics.Bytes;
            _statistics.Reset();
        }
    }
}