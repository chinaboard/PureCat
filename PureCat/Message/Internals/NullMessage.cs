using System;
using System.Collections.Generic;

namespace PureCat.Message.Internals
{
    public class NullMessage : ITransaction, IEvent, IMetric, ITrace, IHeartbeat, ITaggedTransaction, IForkedTransaction
    {
        private static readonly NullMessage _nullMessage = new NullMessage();

        public static ITransaction TRANSACTION { get { return _nullMessage; } }

        public static IEvent EVENT { get { return _nullMessage; } }

        public static IMetric METRIC { get { return _nullMessage; } }

        public static ITrace TRACE { get { return _nullMessage; } }

        public static IHeartbeat HEARTBEAT { get { return _nullMessage; } }

        public static ITaggedTransaction TAGGEDTRANSACTION { get { return _nullMessage; } }

        public static IForkedTransaction FORKEDTRANSACTION { get { return _nullMessage; } }

        #region member
        public IList<IMessage> Children
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Data
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public long DurationInMicros
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public long DurationInMillis
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ParentMessageId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string RootMessageId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Standalone
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Status
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Tag
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public long Timestamp
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ForkedMessageId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ITransaction AddChild(IMessage message)
        {
            throw new NotImplementedException();
        }

        public void AddData(string keyValuePairs)
        {
            throw new NotImplementedException();
        }

        public void AddData(string key, object value)
        {
            throw new NotImplementedException();
        }

        public void Bind(string tag, string childMessageId, string title)
        {
            throw new NotImplementedException();
        }

        public void Complete()
        {
            throw new NotImplementedException();
        }

        public bool HasChildren()
        {
            throw new NotImplementedException();
        }

        public bool IsCompleted()
        {
            throw new NotImplementedException();
        }

        public bool IsSuccess()
        {
            throw new NotImplementedException();
        }

        public void SetStatus(Exception e)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Fork()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
