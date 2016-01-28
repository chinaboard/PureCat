using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Xml.Serialization;

namespace PureCat.Message.Spi.Heartbeat
{
    [XmlRoot("memory")]
    public class MemoryInfo : IRefresh, IXmlSerializable
    {
        private int[] gcCounts = new int[GC.MaxGeneration + 1];
        private static PerformanceCounter _memory = new PerformanceCounter("Memory", "Available Bytes");
        public MemoryInfo()
        {
            GCInfoList = new List<GCInfo>();
            GCInfoList.Add(new GCInfo());
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                gcCounts[i] = GC.CollectionCount(i);
            }
        }

        [XmlAttribute("max")]
        public long Max { get; set; }

        [XmlAttribute("total")]
        public long Total { get; set; }

        [XmlAttribute("free")]
        public long Free { get; set; }

        [XmlAttribute("heap-usage")]
        public long HeapUse { get; set; }

        [XmlAttribute("non-heap-usage")]
        public long HeapUnUse { get; set; }

        [XmlElement("gc")]
        public List<GCInfo> GCInfoList { get; set; }

        public void Refresh()
        {
            Max = (long)GetMemory() / 1024;
            var p = Process.GetCurrentProcess();
            Total = p.PrivateMemorySize64 / 1024;
            HeapUse = GC.GetTotalMemory(false) / 1024;
            HeapUnUse = (Total - HeapUse) / 1024;
            Free = (long)_memory.NextValue() / 1024;

            GCInfoList.Clear();
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                var gcInfo = new GCInfo();
                gcInfo.Name = "Gen_" + i.ToString();
                var time = GC.CollectionCount(i);
                gcInfo.Count = time - gcCounts[i];
                gcInfo.Time = time;
                gcCounts[i] = time;
            }
        }


        private float GetMemory()
        {
            float capacity = 0;
            ManagementClass cimobject1 = new ManagementClass("Win32_PhysicalMemory");
            ManagementObjectCollection moc1 = cimobject1.GetInstances();

            foreach (ManagementObject mo1 in moc1)
            {
                capacity += (float)Convert.ToDouble(mo1.Properties["Capacity"].Value);
            }
            moc1.Dispose();
            cimobject1.Dispose();
            return capacity;
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
            writer.WriteAttributeString("max", Max.ToString());
            writer.WriteAttributeString("total", Total.ToString());
            writer.WriteAttributeString("free", Free.ToString());
            writer.WriteAttributeString("heap-usage", HeapUse.ToString());
            writer.WriteAttributeString("non-heap-usage", HeapUnUse.ToString());
            foreach (var item in GCInfoList)
            {
                writer.WriteStartElement("gc");
                if (!string.IsNullOrEmpty(item.Name))
                    writer.WriteAttributeString("name", item.Name.ToString());
                writer.WriteAttributeString("count", item.Count.ToString());
                writer.WriteAttributeString("time", item.Time.ToString());
                writer.WriteEndElement();
            }
        }

        #endregion IXmlSerializable
    }
}