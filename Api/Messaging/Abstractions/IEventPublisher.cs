namespace Api.Messaging.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(string queue, Guid messageId, ReadOnlyMemory<byte> body, CancellationToken ct);
}
