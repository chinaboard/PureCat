
namespace PureCat.Message.Internals
{
    public class NullTrace : AbstractMessage, ITrace
    {
        public NullTrace()
            : base(null, null)
        {
        }
    }
}
