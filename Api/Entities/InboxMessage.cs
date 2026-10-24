namespace Api.Entities;

public class InboxMessage
{
    public long Id { get; init; }
    public Guid MessageId { get; private set; }
    public DateTime ProcessedAtUtc { get; private set; }

    public InboxMessage() { }

    public InboxMessage(Guid messageId)
    {
        MessageId = messageId;
        ProcessedAtUtc = DateTime.UtcNow;
    }
}
