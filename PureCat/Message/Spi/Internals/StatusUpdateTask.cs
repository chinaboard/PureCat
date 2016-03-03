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

                if (!PureCat.IsInitialized())
                {
                    Thread.Sleep(5000);
                    continue;
                }

                PureCat.DoTransaction("System", "Status", () =>
                {
                    _nodeInfo.Refresh();
                    PureCat.LogHeartbeat("Heartbeat", AppEnv.IP, PureCatConstants.SUCCESS, XmlHelper.XmlSerialize(_nodeInfo, Encoding.UTF8));
                    PureCat.LogEvent("System", $"Cat.Version : {PureCat.Version}", PureCatConstants.SUCCESS, PureCat.Version);
                });

                Thread.Sleep(60000);
            }
        }
    }
}