using System;

namespace PureCat.Configuration
{
    /// <summary>
    /// 描述记录当前系统日志的目标Cat服务器
    /// </summary>
    public class Server
    {
        public Server(string ip, int port = 2280)
        {
            Ip = ip;
            Port = port;
        }

        /// <summary>
        /// Cat服务器IP
        /// </summary>
        public string Ip { get; private set; }

        /// <summary>
        /// Cat服务器端口
        /// </summary>
        public int Port { get; private set; } = 2280;

        /// <summary>
        /// Cat服务http端口
        /// </summary>
        public int WebPort { get; private set; } = 8080;

        /// <summary>
        /// Cat服务器是否有效，默认有效
        /// </summary>
        public bool Enabled { get; set; } = true;

        public override bool Equals(object obj)
        {
            Server peer = (Server)obj;
            if (peer == null)
            {
                return false;
            }
            if (ReferenceEquals(peer, this))
            {
                return true;
            }
            bool ret = false;
            ret = (Ip == peer.Ip);
            if (!ret) return ret;
            ret = (Enabled == peer.Enabled);
            if (!ret) return ret;
            ret = (Port == peer.Port);
            if (!ret) return ret;
            ret = Ip.Equals(peer.Ip);
            if (!ret) return ret;
            return ret;
        }

        public override int GetHashCode()
        {
            int result = 17;
            int ret = GetType().GetHashCode();
            result = 37 * result + ret;
            ret = Port;
            result = 37 * result + ret;
            ret = WebPort;
            result = 37 * result + ret;
            ret = Ip.GetHashCode();
            result = 37 * result + ret;
            return result;
        }
    }
}