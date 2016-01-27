using System.Globalization;
using System;
using System.Collections.Generic;
using System.Text;

namespace PureCat.Message.Internals
{
    [Obsolete]
    public class MessageId
    {
        private readonly string _mDomain;

        private readonly int _mIndex;

        private readonly string _mIpAddressInHex;

        private readonly long _mTimestamp;

        internal MessageId(string domain, string ipAddressInHex, long timestamp, int index)
        {
            _mDomain = domain;
            _mIpAddressInHex = ipAddressInHex;
            _mTimestamp = timestamp;
            _mIndex = index;
        }

        public string Domain
        {
            get { return _mDomain; }
        }

        public int Index
        {
            get { return _mIndex; }
        }

        public string IpAddressInHex
        {
            get { return _mIpAddressInHex; }
        }

        public long Timestamp
        {
            get { return _mTimestamp; }
        }

        public static MessageId Parse(string messageId)
        {
            IList<String> list = messageId.Split('-');
            int len = list.Count;

            if (len >= 4)
            {
                string ipAddressInHex = list[len - 3];
                long timestamp = (Int64.Parse(list[len - 2], NumberStyles.Integer));
                int index = Int32.Parse(list[len - 1]);
                string domain;

                if (len > 4)
                {
                    // allow domain contains '-'
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < len - 3; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append('-');
                        }

                        sb.Append(list[i]);
                    }

                    domain = sb.ToString();
                }
                else
                {
                    domain = list[0];
                }

                return new MessageId(domain, ipAddressInHex, timestamp, index);
            }

            throw new Exception("Invalid message id format: " + messageId);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(_mDomain.Length + 32);

            sb.Append(_mDomain);
            sb.Append('-');
            sb.Append(_mIpAddressInHex);
            sb.Append('-');
            sb.Append(_mTimestamp);
            sb.Append('-');
            sb.Append(_mIndex);

            return sb.ToString();
        }
    }
}