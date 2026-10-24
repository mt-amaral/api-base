using Api.Configurations;
using Api.Context;
using Api.Dto;
using Api.Dto.Email;
using Api.Messaging.Abstractions;
using Api.Messaging.Contracts;
using Api.Services.Abstractions;

namespace Api.Services;

public class EmailService(ApplicationDbContext context, IOutbox outbox, ILogger<EmailService> logger) : IEmailService
{
    public async Task<(Response<EmailQueuedResponseDto?>, short)> SendAsync(SendEmailRequestDto request, CancellationToken ct)
    {
        try
        {
            var message = new SendEmailMessage(
                Guid.NewGuid(),
                DateTime.UtcNow,
                request.To.Trim(),
                request.Subject.Trim(),
                request.Body);

            outbox.Enqueue(ConfigApp.EmailQueueName, message);
            await context.SaveChangesAsync(ct);

            var response = new EmailQueuedResponseDto(message.MessageId);

            return (new Response<EmailQueuedResponseDto?>(response, "E-mail enfileirado para envio."), 202);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enfileirar e-mail para {To}", request.To);
            return (new Response<EmailQueuedResponseDto?>(null, "Erro ao enfileirar e-mail."), 500);
        }
    }
}
