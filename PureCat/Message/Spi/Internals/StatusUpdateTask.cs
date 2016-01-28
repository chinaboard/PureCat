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
        internal readonly NodeStatusInfo m_nodeInfo = null;

        public StatusUpdateTask(IMessageStatistics mStatistics)
        {
            try
            {
                m_nodeInfo = new NodeStatusInfo(mStatistics);
                m_nodeInfo.HeartBeatExtensions.Add(new CpuInfo());
                m_nodeInfo.HeartBeatExtensions.Add(new NetworkIO());
                m_nodeInfo.HeartBeatExtensions.Add(new DiskIO());
                m_nodeInfo.Refresh();
                m_nodeInfo.HaveAcessRight = true;
            }
            catch
            {
            }
        }

        public void Run(object o)
        {
            while (true)
            {
                if (!m_nodeInfo.HaveAcessRight)
                    break;
                m_nodeInfo.Refresh();
                ITransaction t = PureCat.GetProducer().NewTransaction("System", "Status");
                var xml = XmlHelper.XmlSerialize(m_nodeInfo, Encoding.UTF8);

                Logger.Info(xml);

                PureCat.GetProducer().LogHeartbeat("Heartbeat", AppEnv.IP, "0", xml);
                t.Complete();

                PureCat.GetProducer().LogEvent("System", "Version", "0", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                Thread.Sleep(60000);
            }
        }
    }
}