using Api.Messaging.Abstractions;
using RabbitMQ.Client;

namespace Api.Messaging.Publishers;

public class RabbitMqEventPublisher(RabbitMqConnection connection, ILogger<RabbitMqEventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync(string queue, Guid messageId, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        await using var channel = await connection.CreateChannelAsync(ct);

        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId.ToString()
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);

        logger.LogInformation("Mensagem {MessageId} publicada na fila {Queue}", messageId, queue);
    }
}
