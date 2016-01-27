using System.Diagnostics;
using System.Xml.Serialization;

namespace PureCat.Message.Spi.Heartbeat
{
    [XmlRoot("thread")]
    public class ThreadInfo : IRefresh
    {
        [XmlAttribute("count")]
        public int Count { get; set; }

        [XmlAttribute("daemon-count")]
        public int DaemonCount { get; set; }

        [XmlAttribute("peek-count")]
        public int PeekCount { get; set; }

        [XmlAttribute("total-started-count")]
        public int TotalStartCount { get; set; }

        [XmlAttribute("cat-thread-count")]
        public int CatThreadCount { get; set; }

        [XmlAttribute("pigeon-thread-count")]
        public int PigeonThreadCount { get; set; }

        [XmlAttribute("http-thread-count")]
        public int HttpThreadCount { get; set; }

        [XmlElement("dump")]
        public string Dump { get; set; }

        public void Refresh()
        {
            Count = Process.GetCurrentProcess().Threads.Count;
        }
    }
}