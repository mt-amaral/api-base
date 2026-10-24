using System.Text.Json;
using Api.Configurations;
using Api.Context;
using Api.Entities;
using Api.Messaging.Abstractions;
using Api.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Api.Messaging.Consumers;

public class SendEmailConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<SendEmailConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(stoppingToken);

        await channel.QueueDeclareAsync(ConfigApp.EmailQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleAsync(channel, ea, stoppingToken);

        await channel.BasicConsumeAsync(ConfigApp.EmailQueueName, autoAck: false, consumer, stoppingToken);

        logger.LogInformation("Consumidor da fila {Queue} iniciado", ConfigApp.EmailQueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SendEmailMessage>(ea.Body.Span);
            if (message is null)
            {
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var alreadyProcessed = await context.InboxMessage.AnyAsync(x => x.MessageId == message.MessageId, ct);
            if (alreadyProcessed)
            {
                logger.LogInformation("Mensagem {MessageId} já processada, ignorando", message.MessageId);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
                return;
            }

            await sender.SendAsync(message, ct);

            context.InboxMessage.Add(new InboxMessage(message.MessageId));
            await context.SaveChangesAsync(ct);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar mensagem {MessageId} da fila {Queue}", ea.BasicProperties.MessageId, ConfigApp.EmailQueueName);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct);
        }
    }
}
