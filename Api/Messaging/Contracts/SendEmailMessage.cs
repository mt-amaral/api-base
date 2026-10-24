using Api.Messaging.Abstractions;

namespace Api.Messaging.Contracts;

public record SendEmailMessage(
    Guid MessageId,
    DateTime OccurredAtUtc,
    string To,
    string Subject,
    string Body
) : IMessage;
