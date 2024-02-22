using System;

namespace ChatApp.Model
{
    public readonly struct Message
    {
        public string Sender { get; }
        public string Receiver { get; }
        public DateTime DateSent { get; }
        public string Content { get; }
        public MessageType Type { get; }

        public Message(string sender, string receiver, DateTime dateSent, MessageType type, string content = "")
        {
            Sender = sender;
            Receiver = receiver;
            DateSent = dateSent;
            Type = type;
            Content = content;
        }
    }
}