namespace PureCat.Message.Spi
{
    public interface IMessageStatistics
    {
        long Produced { get; }

        long Overflowed { get; }

        long Bytes { get; }

        void OnSending();

        void OnOverflowed();

        void OnBytes(int size);

        void Reset();
    }
}