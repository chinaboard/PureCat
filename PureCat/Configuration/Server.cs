namespace PureCat.Configuration
{
    /// <summary>
    ///   描述记录当前系统日志的目标Cat服务器
    /// </summary>
    public class Server
    {
        public Server(string ip, int port = 2280)
        {
            Ip = ip;
            Port = port;
        }

        /// <summary>
        ///   Cat服务器IP
        /// </summary>
        public string Ip { get; private set; }

        /// <summary>
        ///   Cat服务器端口
        /// </summary>
        public int Port { get; private set; } = 2280;

        public int WebPort { get; private set; } = 8080;

        /// <summary>
        ///   Cat服务器是否有效，默认有效
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}