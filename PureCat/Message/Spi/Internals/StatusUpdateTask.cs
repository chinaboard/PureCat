using PureCat.Message.Spi.Heartbeat;
using PureCat.Message.Spi.Heartbeat.Extend;
using PureCat.Util;
using System.Text;
using System.Threading;

namespace PureCat.Message.Spi.Internals
{
    public class StatusUpdateTask
    {
        internal readonly NodeStatusInfo _nodeInfo = null;

        public StatusUpdateTask(IMessageStatistics statistics)
        {
            try
            {
                _nodeInfo = new NodeStatusInfo(statistics);
                _nodeInfo.HeartbeatExtensions.Add(new CpuInfo());
                _nodeInfo.HeartbeatExtensions.Add(new NetworkIO());
                _nodeInfo.HeartbeatExtensions.Add(new DiskIO());
                _nodeInfo.Refresh();
                _nodeInfo.HaveAcessRight = true;
            }
            catch
            {
            }
        }

        public void Run(object o)
        {
            while (true)
            {
                if (!_nodeInfo.HaveAcessRight)
                    break;

                if (!PureCatClient.IsInitialized())
                {
                    Thread.Sleep(5000);
                    continue;
                }

                PureCatClient.DoTransaction("System", "Status", () =>
                {
                    _nodeInfo.Refresh();
                    PureCatClient.LogHeartbeat("Heartbeat", AppEnv.IP, PureCatConstants.SUCCESS, XmlHelper.XmlSerialize(_nodeInfo, Encoding.UTF8));
                    PureCatClient.LogEvent("System", $"Cat.Version : {PureCatClient.Version}", PureCatConstants.SUCCESS, PureCatClient.Version);
                });

                Thread.Sleep(60000);
            }
        }
    }
}