using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace PureCat.Message.Spi.Heartbeat.Extend
{
    public class NetworkIO : HeartbeatExtention
    {
        protected Dictionary<string, double> m_dict = null;

        protected List<NetworkAdapter> adappterList = new List<NetworkAdapter>();

        public NetworkIO()
        {
            m_dict = new Dictionary<string, double>();
            var interfaces = GetNetworkInterfaces();

            PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");

            foreach (string name in category.GetInstanceNames())
            {
                if (!interfaces.Select(t => t.Description).Contains(name) || name.ToLower().Contains("vmware") || name.ToLower().Contains("loopback"))
                    continue;
                NetworkAdapter adapter = new NetworkAdapter(interfaces.First(t => t.Description.Contains(name)).Name);
                adapter.NetworkBytesReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", name);
                adapter.NetworkBytesSend = new PerformanceCounter("Network Interface", "Bytes Sent/sec", name);
                adappterList.Add(adapter);			// Add it to ArrayList adapter
            }
        }

        public override Dictionary<string, double> Dict
        {
            get { return m_dict; }
        }

        public override string Id
        {
            get { return "NetWorkIO"; }
        }

        public override void Refresh()
        {
            foreach (var item in adappterList)
            {
                m_dict[item.Name + "_Send"] = item.NetworkBytesSend.NextValue();
                m_dict[item.Name + "_Received"] = item.NetworkBytesReceived.NextValue();
            }
        }

        public static List<NetworkInterface> GetNetworkInterfaces()
        {
            List<NetworkInterface> list = new List<NetworkInterface>();
            List<NetworkInterface> Interfaces = new List<NetworkInterface>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    Interfaces.Add(nic);
                }
            }

            foreach (NetworkInterface nic in Interfaces)
            {
                if (nic.GetIPProperties().GetIPv4Properties() != null)
                    list.Add(nic);
            }
            return list;
        }
    }

    public class NetworkAdapter
    {
        public string Name { get; set; }

        public NetworkAdapter(string name)
        {
            Name = name;
        }

        public PerformanceCounter NetworkBytesSend, NetworkBytesReceived;
    }
}