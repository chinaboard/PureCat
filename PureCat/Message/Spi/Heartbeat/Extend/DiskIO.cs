using System.Collections.Generic;
using System.Diagnostics;

namespace PureCat.Message.Spi.Heartbeat.Extend
{
    public class DiskIO : HeartbeatExtention
    {
        protected Dictionary<string, double> m_dict = null;

        protected PerformanceCounter m_readBytesSec = null;

        protected PerformanceCounter m_writeByteSec = null;
        protected PerformanceCounter m_dataBytesSec = null;

        public DiskIO()
        {
            m_dict = new Dictionary<string, double>();
            m_readBytesSec = new PerformanceCounter("PhysicalDisk", "Disk Reads/sec", "_Total");
            m_writeByteSec = new PerformanceCounter("PhysicalDisk", "Disk Writes/sec", "_Total");
            m_dataBytesSec = new PerformanceCounter("PhysicalDisk", "Disk Transfers/sec", "_Total");
        }

        public override Dictionary<string, double> Dict
        {
            get { return m_dict; }
        }

        public override string Id
        {
            get { return "DiskIO"; }
        }

        public override void Refresh()
        {
            m_dict["Read"] = m_readBytesSec.NextValue();
            m_dict["Write"] = m_writeByteSec.NextValue();
            m_dict["Total"] = m_dataBytesSec.NextValue();
        }
    }
}