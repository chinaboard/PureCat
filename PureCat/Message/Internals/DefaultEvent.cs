using System;

namespace PureCat.Message.Internals
{
    public class DefaultEvent : AbstractMessage, IEvent
    {
        public DefaultEvent(string type, string name)
            : base(type, name)
        {
        }
    }
}