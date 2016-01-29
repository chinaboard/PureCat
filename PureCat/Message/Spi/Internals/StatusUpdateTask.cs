using PureCat.Message.Spi.Heartbeat;
using PureCat.Message.Spi.Heartbeat.Extend;
using PureCat.Util;
using System;
using System.Text;
using System.Threading;

namespace PureCat.Message.Spi.Internals
{
    public class StatusUpdateTask
    {
        internal readonly NodeStatusInfo _nodeInfo = null;

        public StatusUpdateTask(IMessageStatistics mStatistics)
        {
            try
            {
                _nodeInfo = new NodeStatusInfo(mStatistics);
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
                _nodeInfo.Refresh();
                ITransaction t = PureCat.GetProducer().NewTransaction("System", "Status");
                var xml = XmlHelper.XmlSerialize(_nodeInfo, Encoding.UTF8);

                //Logger.Info(xml);

                PureCat.GetProducer().LogHeartbeat("Heartbeat", AppEnv.IP, PureCatConstants.SUCCESS, xml);
                t.Complete();

                PureCat.GetProducer().LogEvent("System", "Version", PureCatConstants.SUCCESS, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                Thread.Sleep(60000);
            }
        }
    }
}