using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace PureCat.Util
{
    public class NetworkInterfaceManager
    {

        private static readonly string _localIp = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Min(p => p.ToString());

        public static string GetLocalHostName()
        {
            return Dns.GetHostName();
        }

        public static string GetLocalHostAddress() => _localIp;

        public static byte[] GetAddressBytes() => Encoding.UTF8.GetBytes(_localIp);
    }
}