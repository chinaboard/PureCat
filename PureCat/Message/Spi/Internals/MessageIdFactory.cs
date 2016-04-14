using PureCat.Util;
using System;
using System.Text;
using System.Threading;

namespace PureCat.Message.Spi.Internals
{
    /// <summary>
    ///   根据域名（配置指定的），系统IP（自动解析的，16进制字符串），时间戳（1970年到当前的小时数）和自增编号组成
    /// </summary>
    public class MessageIdFactory
    {
        private string _domain;
        private int _index;

        private string _ipAddress;
        private long _lastTimestamp;

        public MessageIdFactory()
        {
            _lastTimestamp = Timestamp;
        }

        protected internal long Timestamp
        {
            get { return MilliSecondTimer.CurrentTimeHoursForJava; }
        }

        public string Domain
        {
            set { _domain = value; }
        }

        public string IpAddress
        {
            set { _ipAddress = value; }
        }

        public string GetNextId()
        {
            long timestamp = Timestamp;

            if (timestamp != _lastTimestamp)
            {
                Interlocked.Exchange(ref _index, 0);
                Interlocked.Exchange(ref _lastTimestamp, timestamp);
            }

            int index = Interlocked.Increment(ref _index);

            StringBuilder sb = new StringBuilder(_domain.Length + 32);

            sb.Append(_domain);
            sb.Append('-');
            sb.Append(_ipAddress);
            sb.Append('-');
            sb.Append(timestamp);
            sb.Append('-');
            sb.Append(index);

            return sb.ToString();
        }

        public void Initialize(string domain)
        {
            _domain = domain;

            if (_ipAddress != null) return;

            byte[] bytes = NetworkInterfaceManager.GetAddressBytes();

            StringBuilder sb = new StringBuilder();

            foreach (byte b in bytes)
            {
                sb.Append(((b >> 4) & 0x0F).ToString("x"));
                sb.Append((b & 0x0F).ToString("x"));
            }

            _ipAddress = sb.ToString();
        }
    }
}