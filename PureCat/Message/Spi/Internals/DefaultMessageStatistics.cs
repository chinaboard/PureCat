using System.Threading;

namespace PureCat.Message.Spi.Internals
{
    public class DefaultMessageStatistics : IMessageStatistics
    {
        private long _produced = 0;
        private long _overflowed = 0;
        private long _bytes = 0;

        public long Produced { get { return _produced; } }
        public long Overflowed { get { return _overflowed; } }
        public long Bytes { get { return _bytes; } }

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
            Interlocked.Exchange(ref _bytes, Interlocked.Add(ref _bytes, size));
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _produced, 0);
            Interlocked.Exchange(ref _overflowed, 0);
            Interlocked.Exchange(ref _bytes, 0);
        }
    }
}