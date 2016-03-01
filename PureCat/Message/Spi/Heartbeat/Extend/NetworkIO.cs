using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace PureCat.Message.Spi.Heartbeat.Extend
{
    public class NetworkIO : HeartbeatExtention
    {
        protected Dictionary<string, double> _dict = null;

        protected List<NetworkAdapter> _adappterList = new List<NetworkAdapter>();

        public NetworkIO()
        {
            _dict = new Dictionary<string, double>();
            PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");

            var interfaces = GetNetworkInterfaces();
            var categoryList = category.GetInstanceNames();

            foreach (string name in categoryList)
            {
                var nicName = name.Replace('[', '(').Replace(']', ')');
                if (!interfaces.Select(t => t.Description).Contains(nicName) || nicName.ToLower().Contains("loopback"))
                    continue;
                try
                {
                    NetworkAdapter adapter = new NetworkAdapter(interfaces.First(t => t.Description.Contains(nicName)).Name);
                    adapter.NetworkBytesReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", name);
                    adapter.NetworkBytesSend = new PerformanceCounter("Network Interface", "Bytes Sent/sec", name);
                    _adappterList.Add(adapter);         // Add it to ArrayList adapter
                }
                catch
                {
                    //pass
                }
            }
        }

        public override Dictionary<string, double> Dict
        {
            get { return _dict; }
        }

        public override string Id
        {
            get { return "NetWorkIO"; }
        }

        public override void Refresh()
        {
            _adappterList.ForEach(item =>
            {
                _dict[item.Name + " Bytes Sent/sec"] = item.NetworkBytesSend.NextValue();
                _dict[item.Name + " Bytes Received/sec"] = item.NetworkBytesReceived.NextValue();
            });
        }

        public static List<NetworkInterface> GetNetworkInterfaces()
        {
            List<NetworkInterface> list = new List<NetworkInterface>();
            List<NetworkInterface> Interfaces = new List<NetworkInterface>();

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet && nic.OperationalStatus == OperationalStatus.Up)
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