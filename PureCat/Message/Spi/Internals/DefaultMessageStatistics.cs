using System.Threading;

namespace PureCat.Message.Spi.Internals
{
    public class DefaultMessageStatistics : IMessageStatistics
    {
        private long _produced = 0;
        private long _overflowed = 0;
        private long _bytes = 0;

        public long Produced { get { return Interlocked.CompareExchange(ref _produced, -1, -1); } }
        public long Overflowed { get { return Interlocked.CompareExchange(ref _overflowed, -1, -1); } }
        public long Bytes { get { return Interlocked.CompareExchange(ref _bytes, -1, -1); } }

        public void OnSending(IMessageTree tree)
        {
            Interlocked.Increment(ref _produced);
        }

        public void OnOverflowed(IMessageTree tree)
        {
            Interlocked.Increment(ref _overflowed);
        }

        public void OnBytes(int size)
        {
            Interlocked.Add(ref _bytes, size);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _produced, 0);
            Interlocked.Exchange(ref _overflowed, 0);
            Interlocked.Exchange(ref _bytes, 0);
        }
    }
}