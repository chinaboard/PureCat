using System;

namespace PureCat.Message
{
    public interface IMessageTree
    {
        string Domain { get; set; }


        string HostName { get; set; }


        string IpAddress { get; set; }


        IMessage Message { get; set; }


        string MessageId { get; set; }


        string ParentMessageId { get; set; }


        string RootMessageId { get; set; }


        string SessionToken { get; set; }


        string ThreadGroupName { get; set; }


        string ThreadId { get; set; }


        string ThreadName { get; set; }

        IMessageTree Copy();
    }
}