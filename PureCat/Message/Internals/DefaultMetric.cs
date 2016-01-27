using System;

namespace PureCat.Message.Internals
{
    public class DefaultMetric : AbstractMessage, IMetric
    {
        public DefaultMetric(string type, string name)
            : base(type, name)
        {
        }
    }
}
