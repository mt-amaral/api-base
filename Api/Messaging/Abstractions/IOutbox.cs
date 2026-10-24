namespace Api.Messaging.Abstractions;

public interface IOutbox
{
    void Enqueue<TMessage>(string queue, TMessage message) where TMessage : IMessage;
}
