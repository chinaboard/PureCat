using System;

namespace PureCat.Message.Internals
{
    public class DefaultHeartbeat : AbstractMessage, IHeartbeat
    {
        public DefaultHeartbeat(string type, string name)
            : base(type, name)
        {
        }
    }
}