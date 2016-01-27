using System;

namespace PureCat.Message.Internals
{
    public class DefaultTrace : AbstractMessage, ITrace
    {
        private readonly Action<ITrace> _endCallBack;
        public DefaultTrace(string type, string name, Action<ITrace> endCallBack = null)
            : base(type, name)
        {
            _endCallBack = endCallBack;
        }
        public override void Complete()
        {
            SetCompleted(true);

            if (_endCallBack != null)
            {
                _endCallBack(this);
            }
        }
    }
}
