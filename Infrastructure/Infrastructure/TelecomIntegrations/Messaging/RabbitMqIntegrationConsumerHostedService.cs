using System.Text;
using System.Text.Json;
using Application.Common.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.TelecomIntegrations.Messaging;

/// <summary>Consumes integration events from RabbitMQ and dispatches them via MediatR.</summary>
public sealed class RabbitMqIntegrationConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqIntegrationConsumerHostedService> _logger;

    public RabbitMqIntegrationConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqIntegrationConsumerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("RabbitMQ consumer disabled — using in-process MediatR dispatch.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer failed — retrying in 5s.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_options.ConnectionString),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            _options.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            _options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            _options.Queue,
            _options.Exchange,
            _options.RoutingKey,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var envelope = JsonSerializer.Deserialize<IntegrationMessageEnvelope>(json);
                if (envelope != null)
                {
                    await DispatchAsync(envelope, stoppingToken);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process RabbitMQ message — nacking for requeue.");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(_options.Queue, autoAck: false, consumer, stoppingToken);
        _logger.LogInformation("RabbitMQ consumer listening on queue {Queue}", _options.Queue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task DispatchAsync(IntegrationMessageEnvelope envelope, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        if (string.Equals(envelope.EventType, nameof(TelecomOperationProvisionedNotification), StringComparison.Ordinal))
        {
            var notification = JsonSerializer.Deserialize<TelecomOperationProvisionedNotification>(envelope.PayloadJson);
            if (notification != null)
            {
                await publisher.Publish(notification, cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning("Unknown RabbitMQ event type {EventType}", envelope.EventType);
        }
    }
}
