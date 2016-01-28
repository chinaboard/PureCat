using System.Collections.Generic;

namespace PureCat.Message
{

    public interface ITaggedTransaction : ITransaction
    {
        void Bind(string tag, string childMessageId, string title);
        string ParentMessageId { get; }
        string RootMessageId { get; }
        string Tag { get; }
        void Start();
    }
}