﻿using System;
using System.Text;
using PureCat.Message.Spi.Codec;
using PureCat.Util;
using PureCat.Message.Spi;

namespace PureCat.Message.Internals
{
    [Serializable]
    public abstract class AbstractMessage : IMessage
    {
        private readonly string _mName;
        private readonly string _mType;
        private bool _mCompleted;
        private StringBuilder _mData;
        private IMessageManager _messageManager;

        private string _mStatus = "0";

        protected AbstractMessage(string type, string name, IMessageManager messageManager = null)
        {
            _mType = type;
            _mName = name;
            _messageManager = messageManager;
            TimestampInMicros = MilliSecondTimer.CurrentTimeMicros();
        }

        /// <summary>
        ///   其实是Ticks除以10
        /// </summary>
        protected long TimestampInMicros { get; private set; }

        #region IMessage Members

        public IMessageManager MessageManager { get { return _messageManager; } }

        public string Data
        {
            get { return _mData == null || _mData.Length == 0 ? string.Empty : _mData.ToString(); }
        }

        public string Name
        {
            get { return _mName; }
        }

        public string Status
        {
            get { return _mStatus; }

            set { _mStatus = value; }
        }
        /// <summary>
        ///   其实是Ticks除以10000
        /// </summary>
        public long Timestamp
        {
            get { return TimestampInMicros / 1000L; }
            set { TimestampInMicros = value * 1000L; }
        }

        public string Type
        {
            get { return _mType; }
        }

        public void AddData(string keyValuePairs)
        {
            if (_mData == null)
            {
                _mData = new StringBuilder(keyValuePairs);
            }
            else
            {
                _mData.Append(keyValuePairs);
            }
        }

        public void AddData(string key, Object value)
        {
            if (_mData == null)
            {
                _mData = new StringBuilder();
            }
            else if (_mData.Length > 0)
            {
                _mData.Append('&');
            }

            _mData.Append(key).Append('=').Append(value);
        }

        public virtual void Complete()
        {
            SetCompleted(true);
        }

        public bool IsCompleted()
        {
            return _mCompleted;
        }

        public bool IsSuccess()
        {
            return "0" == _mStatus;
        }

        public void SetStatus(Exception e)
        {
            _mStatus = e.GetType().FullName;
        }

        #endregion

        protected void SetCompleted(bool completed)
        {
            _mCompleted = completed;
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