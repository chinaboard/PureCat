using PureCat.Message.Spi.Heartbeat;
using PureCat.Message.Spi.Heartbeat.Extend;
using PureCat.Util;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task Run()
        {
            while (true)
            {
                if (!_nodeInfo.HaveAcessRight)
                    break;

                if (!PureCatClient.IsInitialized())
                {
#if NET40
                    await TaskEx.Delay(5000);
#else
                    await Task.Delay(5000);
#endif
                    continue;
                }

                PureCatClient.DoTransaction("System", "Status", () =>
                {
                    _nodeInfo.Refresh();
                    PureCatClient.GetProducer().LogHeartbeat("Heartbeat", AppEnv.IP, PureCatConstants.SUCCESS, XmlHelper.XmlSerialize(_nodeInfo, Encoding.UTF8));
                    PureCatClient.GetProducer().LogEvent("System", $"PureCat.Version : {PureCatClient.Version}", PureCatConstants.SUCCESS, PureCatClient.Version);
                });

#if NET40
                await TaskEx.Delay(60000);
#else
                await Task.Delay(60000);
#endif
            }
        }
    }
}