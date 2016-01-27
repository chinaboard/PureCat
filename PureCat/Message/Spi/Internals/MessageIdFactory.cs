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
        private string _mDomain;
        private int _mIndex;

        private string _mIpAddress;
        private long _mLastTimestamp;

        public MessageIdFactory()
        {
            _mLastTimestamp = Timestamp;
        }

        protected internal long Timestamp
        {
            get { return MilliSecondTimer.CurrentTimeHoursForJava(); }
        }

        public string Domain
        {
            set { _mDomain = value; }
        }

        public string IpAddress
        {
            set { _mIpAddress = value; }
        }

        public string GetNextId()
        {
            long timestamp = Timestamp;

            if (timestamp != _mLastTimestamp)
            {
                Interlocked.Exchange(ref _mIndex, 0);
                Interlocked.Exchange(ref _mLastTimestamp, timestamp);
            }

            int index = Interlocked.Increment(ref _mIndex);

            StringBuilder sb = new StringBuilder(_mDomain.Length + 32);

            sb.Append(_mDomain);
            sb.Append('-');
            sb.Append(_mIpAddress);
            sb.Append('-');
            sb.Append(timestamp);
            sb.Append('-');
            sb.Append(index);

            return sb.ToString();
        }

        public void Initialize(string domain)
        {
            _mDomain = domain;

            if (_mIpAddress != null) return;

            byte[] bytes = NetworkInterfaceManager.GetAddressBytes();

            StringBuilder sb = new StringBuilder();

            foreach (byte b in bytes)
            {
                sb.Append(((b >> 4) & 0x0F).ToString("x"));
                sb.Append((b & 0x0F).ToString("x"));
            }

            _mIpAddress = sb.ToString();
        }
    }
}