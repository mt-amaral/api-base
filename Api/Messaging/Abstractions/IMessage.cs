namespace Api.Messaging.Abstractions;

public interface IMessage
{
    Guid MessageId { get; }
    DateTime OccurredAtUtc { get; }
}
