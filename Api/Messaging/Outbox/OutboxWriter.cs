using System.Text.Json;
using Api.Context;
using Api.Entities;
using Api.Messaging.Abstractions;

namespace Api.Messaging.Outbox;

public class OutboxWriter(ApplicationDbContext context) : IOutbox
{
    public void Enqueue<TMessage>(string queue, TMessage message) where TMessage : IMessage
    {
        var payload = JsonSerializer.Serialize(message);

        var outboxMessage = new OutboxMessage(
            message.MessageId,
            queue,
            typeof(TMessage).FullName ?? typeof(TMessage).Name,
            payload,
            message.OccurredAtUtc);

        context.OutboxMessage.Add(outboxMessage);
    }
}
