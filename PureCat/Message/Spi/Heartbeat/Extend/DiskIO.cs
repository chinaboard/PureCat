using System.Collections.Generic;
using System.Diagnostics;

namespace PureCat.Message.Spi.Heartbeat.Extend
{
    public class DiskIO : HeartbeatExtention
    {
        protected Dictionary<string, double> _dict = null;

        protected PerformanceCounter _readBytesSec = null;

        protected PerformanceCounter _writeByteSec = null;
        protected PerformanceCounter _dataBytesSec = null;

        public DiskIO()
        {
            _dict = new Dictionary<string, double>();
            _readBytesSec = new PerformanceCounter("PhysicalDisk", "Disk Reads/sec", "_Total");
            _writeByteSec = new PerformanceCounter("PhysicalDisk", "Disk Writes/sec", "_Total");
            _dataBytesSec = new PerformanceCounter("PhysicalDisk", "Disk Transfers/sec", "_Total");
        }

        public override Dictionary<string, double> Dict
        {
            get { return _dict; }
        }

        public override string Id
        {
            get { return "DiskIO"; }
        }

        public override void Refresh()
        {
            _dict["Read"] = _readBytesSec.NextValue();
            _dict["Write"] = _writeByteSec.NextValue();
            _dict["Total"] = _dataBytesSec.NextValue();
        }
    }
}