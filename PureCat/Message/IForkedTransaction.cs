namespace PureCat.Message
{
    public interface IForkedTransaction : ITransaction
    {
        void Fork();
        string ForkedMessageId { get; }
    }
}
