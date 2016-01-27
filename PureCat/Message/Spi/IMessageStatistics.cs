namespace PureCat.Message.Spi
{
    public interface IMessageStatistics
    {
        long Produced { get; }

        long Overflowed { get; }

        long Bytes { get; }

        void OnSending(IMessageTree tree);

        void OnOverflowed(IMessageTree tree);

        void OnBytes(int size);

        void Reset();
    }
}