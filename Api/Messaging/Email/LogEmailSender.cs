using Api.Messaging.Abstractions;
using Api.Messaging.Contracts;

namespace Api.Messaging.Email;

public class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(SendEmailMessage message, CancellationToken ct)
    {
        logger.LogInformation(
            "E-mail {MessageId} enviado para {To} | assunto: {Subject}",
            message.MessageId, message.To, message.Subject);

        return Task.CompletedTask;
    }
}
