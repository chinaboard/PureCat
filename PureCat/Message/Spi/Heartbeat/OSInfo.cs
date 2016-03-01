using Microsoft.VisualBasic.Devices;
using System;
using System.Diagnostics;
using System.Xml.Serialization;

namespace PureCat.Message.Spi.Heartbeat
{
    [XmlRoot("os")]
    public class OSInfo : IRefresh
    {
        private ComputerInfo _computer = null;

        public OSInfo()
        {
            _computer = new ComputerInfo();
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("arch")]
        public string Arch { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("available-processors")]
        public int ProcessCount { get; set; }

        [XmlAttribute("system-load-average")]
        public float AvgLoad { get; set; }

        [XmlAttribute("process-time")]
        public long ProcessTime { get; set; }

        [XmlAttribute("total-physical-memory")]
        public ulong TotalMemory { get; set; }

        [XmlAttribute("free-physical-memory")]
        public ulong FreeMemory { get; set; }

        [XmlAttribute("committed-virtual-memory")]
        public ulong CommitedMemory { get; set; }

        [XmlAttribute("total-swap-space")]
        public ulong TotalSwapSpace { get; set; }

        [XmlAttribute("free-swap-space")]
        public ulong FreeSwapSpace { get; set; }

        public void Refresh()
        {
            Name = _computer.OSFullName;
            Arch = _computer.OSPlatform;
            Version = _computer.OSVersion;
            ProcessCount = Environment.ProcessorCount;
            TotalMemory = _computer.TotalPhysicalMemory;
            FreeMemory = _computer.TotalPhysicalMemory - _computer.AvailablePhysicalMemory;
            ProcessTime = Process.GetCurrentProcess().TotalProcessorTime.Ticks / TimeSpan.TicksPerSecond;
        }
    }
}