using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PureCat.Message.Spi.Heartbeat.Extend
{
    public class CpuInfo : HeartbeatExtention
    {
        protected PerformanceCounter m_cpu = null;
        protected Dictionary<string, double> m_dict = null;

        public CpuInfo()
        {
            m_cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            m_dict = new Dictionary<string, double>();
        }

        public override string Id
        {
            get { return "Cpu"; }
        }

        public override void Refresh()
        {
            m_dict.Clear();
            float percentage = m_cpu.NextValue();
            m_dict["Percentage"] = Math.Round(percentage, 2, MidpointRounding.AwayFromZero);
        }

        public override Dictionary<string, double> Dict
        {
            get { return m_dict; }
        }
    }
}