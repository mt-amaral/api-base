using Api.Messaging.Contracts;

namespace Api.Messaging.Abstractions;

public interface IEmailSender
{
    Task SendAsync(SendEmailMessage message, CancellationToken ct);
}
