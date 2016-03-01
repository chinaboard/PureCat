using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PureCat.Message.Spi.Heartbeat.Extend
{
    public class CpuInfo : HeartbeatExtention
    {
        protected PerformanceCounter _cpu = null;
        protected Dictionary<string, double> _dict = null;

        public CpuInfo()
        {
            _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _dict = new Dictionary<string, double>();
        }

        public override string Id
        {
            get { return "Cpu"; }
        }

        public override void Refresh()
        {
            _dict.Clear();
            float percentage = _cpu.NextValue();
            _dict["Percentage"] = Math.Round(percentage, 2, MidpointRounding.AwayFromZero);
        }

        public override Dictionary<string, double> Dict
        {
            get { return _dict; }
        }
    }
}