using System.Text;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Publishing;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

public sealed class RabbitMqEventPublisher : IEventNotificationPublisher, IAsyncDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private readonly bool _isConnected;

    private const string ExchangeName = "sales.events";

    public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;

        var connectionString = configuration["RabbitMq:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("RabbitMq:ConnectionString not configured. Event publishing to broker is disabled");
            return;
        }

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
            _isConnected = true;
            _logger.LogInformation("RabbitMQ event publisher connected to {ConnectionString}", connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Event publishing to broker is disabled");
            _isConnected = false;
        }
    }

    public async Task PublishAsync<T>(string eventName, T message, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isConnected || _channel is null)
            return;

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, SalesEventJsonSerializerOptions.Default));
        await PublishBodyAsync(_channel, eventName, body, cancellationToken);
    }

    public async Task PublishRawAsync(string eventName, string jsonPayload, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _channel is null)
            throw new InvalidOperationException("RabbitMQ publisher is not connected");

        var body = Encoding.UTF8.GetBytes(jsonPayload);
        await PublishBodyAsync(_channel, eventName, body, cancellationToken);
    }

    private async Task PublishBodyAsync(IChannel channel, string eventName, byte[] body, CancellationToken cancellationToken)
    {
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Type = eventName
        };

        var routingKey = $"sale.{eventName}";
        await channel.BasicPublishAsync(ExchangeName, routingKey, false, properties, body, cancellationToken);
        _logger.LogDebug("Published {EventName} to RabbitMQ exchange {Exchange} with routing key {RoutingKey}",
            eventName, ExchangeName, routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
