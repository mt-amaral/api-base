using System.Text;
using Api.Configurations;
using Api.Context;
using Api.Messaging.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Api.Messaging.Outbox;

public class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Relay do outbox iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no relay do outbox");
            }

            await Task.Delay(TimeSpan.FromSeconds(ConfigApp.OutboxPollSeconds), stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var pending = await context.OutboxMessage
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.Id)
            .Take(ConfigApp.OutboxBatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        foreach (var message in pending)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message.Payload);
                await publisher.PublishAsync(message.Queue, message.MessageId, body, ct);
                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                message.RegisterFailure();
                logger.LogError(ex, "Erro ao publicar mensagem {MessageId} do outbox", message.MessageId);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
