using System.Collections.Generic;
using System.Xml.Serialization;

namespace PureCat.Message.Spi.Heartbeat.Extend
{
    public abstract class HeartbeatExtention : IRefresh
    {
        public abstract Dictionary<string, double> Dict { get; }

        [XmlAttribute("id")]
        public abstract string Id { get; }

        public abstract void Refresh();
    }
}