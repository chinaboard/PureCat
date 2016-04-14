using System;
using System.Text;
using PureCat.Message.Spi.Codec;
using PureCat.Util;
using PureCat.Message.Spi;

namespace PureCat.Message.Internals
{
    
    public abstract class AbstractMessage : IMessage
    {
        private readonly string _name;
        private readonly string _type;
        private bool _completed;
        private StringBuilder _data;
        private IMessageManager _messageManager;

        protected AbstractMessage(string type, string name, IMessageManager messageManager = null)
        {
            _type = type;
            _name = name;
            _messageManager = messageManager;
            TimestampInMicros = MilliSecondTimer.CurrentTimeMicros;
        }

        /// <summary>
        ///   其实是Ticks除以10
        /// </summary>
        protected long TimestampInMicros { get; private set; }

        #region IMessage Members

        public IMessageManager MessageManager { get { return _messageManager; } }

        public string Data { get { return _data?.Length == null ? string.Empty : _data.ToString(); } }

        public string Name { get { return _name; } }

        public string Status { get; set; } = PureCatConstants.SUCCESS;
        /// <summary>
        ///   其实是Ticks除以10000
        /// </summary>
        public long Timestamp { get { return TimestampInMicros / 1000L; } set { TimestampInMicros = value * 1000L; } }

        public string Type { get { return _type; } }

        public void AddData(string keyValuePairs)
        {
            if (_data == null)
            {
                _data = new StringBuilder(keyValuePairs);
            }
            else
            {
                _data.Append(keyValuePairs);
            }
        }

        public void AddData(string key, Object value)
        {
            if (_data == null)
            {
                _data = new StringBuilder();
            }
            else if (_data.Length > 0)
            {
                _data.Append('&');
            }

            _data.Append(key).Append('=').Append(value);
        }

        public virtual void Complete()
        {
            SetCompleted(true);
        }

        public bool IsCompleted()
        {
            return _completed;
        }

        public bool IsSuccess()
        {
            return PureCatConstants.SUCCESS == Status;
        }

        public void SetStatus(Exception e)
        {
            Status = e.GetType().FullName;
        }

        #endregion

        protected void SetCompleted(bool completed)
        {
            _completed = completed;
        }

        public override string ToString()
        {
            PlainTextMessageCodec codec = new PlainTextMessageCodec();
            ChannelBuffer buf = new ChannelBuffer(8192);

            codec.EncodeMessage(this, buf);
            buf.Reset();

            return buf.ToString();
        }
    }
}