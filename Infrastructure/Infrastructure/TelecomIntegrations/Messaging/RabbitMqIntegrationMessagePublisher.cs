using System.Text;
using System.Text.Json;
using Application.Common.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infrastructure.TelecomIntegrations.Messaging;

public sealed class RabbitMqIntegrationMessagePublisher : IIntegrationMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqIntegrationMessagePublisher> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqIntegrationMessagePublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqIntegrationMessagePublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(
        string eventType,
        string payloadJson,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await EnsureChannelAsync(cancellationToken);

        var envelope = new IntegrationMessageEnvelope
        {
            EventType = eventType,
            PayloadJson = payloadJson,
            CorrelationId = correlationId,
            OccurredAtUtc = DateTime.UtcNow,
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = correlationId,
            Type = eventType,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };

        await _channel!.BasicPublishAsync(
            _options.Exchange,
            _options.RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogDebug("Published integration event {EventType} to RabbitMQ", eventType);
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel?.IsOpen == true)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel?.IsOpen == true)
            {
                return;
            }

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_options.ConnectionString),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                _options.Exchange,
                ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(
                _options.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await _channel.QueueBindAsync(
                _options.Queue,
                _options.Exchange,
                _options.RoutingKey,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _initLock.Dispose();
    }
}
