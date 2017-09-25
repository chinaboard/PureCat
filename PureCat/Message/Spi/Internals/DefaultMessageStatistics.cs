using System.Threading;

namespace PureCat.Message.Spi.Internals
{
    public class DefaultMessageStatistics : IMessageStatistics
    {
        private long _produced = 0;
        private long _overflowed = 0;
        private long _bytes = 0;

        public long Produced => Interlocked.Exchange(ref _produced, 0);
        public long Overflowed => Interlocked.Exchange(ref _overflowed, 0);
        public long Bytes => Interlocked.Exchange(ref _bytes, 0);

        public void OnSending()
        {
            Interlocked.Increment(ref _produced);
        }

        public void OnOverflowed()
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