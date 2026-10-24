namespace Api.Entities;

public class OutboxMessage
{
    public long Id { get; init; }
    public Guid MessageId { get; private set; }
    public string Queue { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public int Attempts { get; private set; }

    public OutboxMessage() { }

    public OutboxMessage(Guid messageId, string queue, string type, string payload, DateTime occurredAtUtc)
    {
        MessageId = messageId;
        Queue = queue;
        Type = type;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
    }

    public void MarkProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public void RegisterFailure()
    {
        Attempts++;
    }
}
