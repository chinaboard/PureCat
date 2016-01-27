using System.Xml.Serialization;

namespace PureCat.Message.Spi.Heartbeat
{
    [XmlRoot("disk-volume")]
    public class DiskInfo
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("total")]
        public long Total { get; set; }

        [XmlAttribute("free")]
        public long Free { get; set; }

        [XmlAttribute("usable")]
        public long Use { get; set; }
    }
}