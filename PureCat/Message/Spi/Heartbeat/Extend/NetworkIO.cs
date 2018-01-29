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

            var categoryList = category.GetInstanceNames();

            foreach (string name in categoryList)
            {
                if (name.ToLower().Contains("loopback"))
                    continue;
                try
                {
                    NetworkAdapter adapter = new NetworkAdapter(name);
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
                _dict[item.Name + " MBytes Sent/sec"] = item.NetworkBytesSend.NextValue() / 1024 / 1024;               //MB
                _dict[item.Name + " MBytes Received/sec"] = item.NetworkBytesReceived.NextValue() / 1024 / 1024;       //MB
            });
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