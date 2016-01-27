namespace PureCat.Message.Spi.IO
{
    public interface IMessageSender
    {
        bool HasSendingMessage { get; }

        void Initialize();

        void Send(IMessageTree tree);

        void Shutdown();
    }
}