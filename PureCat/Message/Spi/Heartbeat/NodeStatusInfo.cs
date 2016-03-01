using PureCat.Message.Spi.Heartbeat.Extend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

namespace PureCat.Message.Spi.Heartbeat
{
    [XmlRoot("status")]
    public class NodeStatusInfo : IRefresh, IXmlSerializable
    {
        internal bool HaveAcessRight = false;

        private XmlSerializerNamespaces ns = new XmlSerializerNamespaces();

        public NodeStatusInfo()
        {
        }

        public NodeStatusInfo(IMessageStatistics statistics)
        {
            RuntimeInfo = new RuntimeInfo();
            OSInfo = new OSInfo();
            DiskInfoList = new List<DiskInfo>();
            MemoryInfo = new MemoryInfo();
            ThreadInfo = new ThreadInfo();
            MessageInfo = new MessageInfo(statistics);
            HeartbeatExtensions = new List<HeartbeatExtention>();
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            ns.Add("", "");
        }

        [XmlAttribute("timestamp")]
        public string Timestamp { get; set; }

        public RuntimeInfo RuntimeInfo { get; set; }
        public OSInfo OSInfo { get; set; }

        public List<DiskInfo> DiskInfoList { get; set; }

        public MemoryInfo MemoryInfo { get; set; }

        public ThreadInfo ThreadInfo { get; set; }

        public MessageInfo MessageInfo { get; set; }

        public List<HeartbeatExtention> HeartbeatExtensions { get; set; }

        public void Refresh()
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            RuntimeInfo.Refresh();
            OSInfo.Refresh();

            DiskInfoList.Clear();
            var drives = DriveInfo.GetDrives().ToList();
            drives.ForEach(drive =>
            {

                if (drive.IsReady)
                {
                    DiskInfoList.Add(new DiskInfo()
                    {
                        Id = drive.Name,
                        Free = drive.AvailableFreeSpace / 1024,
                        Total = drive.TotalSize / 1024,
                        Use = (drive.TotalSize - drive.AvailableFreeSpace) / 1024
                    });
                }
            });
            MemoryInfo.Refresh();
            ThreadInfo.Refresh();
            MessageInfo.Refresh();
            HeartbeatExtensions.ForEach(item => item.Refresh());
        }

        #region IXmlSerializable

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer serializer = null;

            writer.WriteAttributeString("timestamp", Timestamp);

            serializer = new XmlSerializer(typeof(RuntimeInfo));
            serializer.Serialize(writer, RuntimeInfo, ns);

            serializer = new XmlSerializer(typeof(OSInfo));
            serializer.Serialize(writer, OSInfo, ns);

            writer.WriteStartElement("disk");
            DiskInfoList.ForEach(item =>
            {
                serializer = new XmlSerializer(typeof(DiskInfo));
                serializer.Serialize(writer, item, ns);
            });
            writer.WriteEndElement();

            serializer = new XmlSerializer(typeof(MemoryInfo));
            serializer.Serialize(writer, MemoryInfo, ns);

            serializer = new XmlSerializer(typeof(ThreadInfo));
            serializer.Serialize(writer, ThreadInfo, ns);

            serializer = new XmlSerializer(typeof(MessageInfo));
            serializer.Serialize(writer, MessageInfo, ns);

            HeartbeatExtensions.ForEach(item =>
            {
                writer.WriteStartElement("extension");
                writer.WriteAttributeString("id", item.Id);
                foreach (var item2 in item.Dict)
                {
                    writer.WriteStartElement("extensionDetail");
                    writer.WriteAttributeString("id", item2.Key.ToString());
                    writer.WriteAttributeString("value", item2.Value.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

            });
        }

        #endregion IXmlSerializable
    }
}