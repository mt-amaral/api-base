using Api.Messaging;
using Api.Messaging.Abstractions;
using Api.Messaging.Consumers;
using Api.Messaging.Email;
using Api.Messaging.Outbox;
using Api.Messaging.Publishers;

namespace Api.Configurations.Setup;

public static class MessagingSetup
{
    public static IServiceCollection AddMessagingSetup(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddScoped<IOutbox, OutboxWriter>();
        services.AddScoped<IEmailSender, LogEmailSender>();
        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<SendEmailConsumer>();

        return services;
    }
}
