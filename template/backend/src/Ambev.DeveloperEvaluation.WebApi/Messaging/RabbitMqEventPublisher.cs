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
    private readonly string? _connectionString;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    private const string ExchangeName = "sales.events";
    private const int MaxRetryAttempts = 3;

    public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;
        _connectionString = configuration["RabbitMq:ConnectionString"];

        if (string.IsNullOrWhiteSpace(_connectionString))
            _logger.LogWarning("RabbitMq:ConnectionString not configured. Event publishing to broker is disabled");
    }

    public bool IsConnected => _connection?.IsOpen == true && _channel?.IsOpen == true;

    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return false;

        try
        {
            await EnsureConnectedAsync(cancellationToken);
            return IsConnected;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "RabbitMQ health check failed");
            return false;
        }
    }

    public async Task PublishAsync<T>(string eventName, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return;

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, SalesEventJsonSerializerOptions.Default));
        await PublishWithRetryAsync(eventName, body, cancellationToken);
    }

    public async Task PublishRawAsync(string eventName, string jsonPayload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("RabbitMQ connection string is not configured");

        var body = Encoding.UTF8.GetBytes(jsonPayload);
        await PublishWithRetryAsync(eventName, body, cancellationToken);
    }

    private async Task PublishWithRetryAsync(string eventName, byte[] body, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                await EnsureConnectedAsync(cancellationToken);
                await PublishBodyAsync(eventName, body, cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                await DisposeConnectionAsync();

                if (attempt == MaxRetryAttempts)
                    break;

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning(ex,
                    "RabbitMQ publish attempt {Attempt}/{MaxAttempts} failed for {EventName}. Retrying in {DelaySeconds}s",
                    attempt, MaxRetryAttempts, eventName, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Failed to publish {eventName} to RabbitMQ after {MaxRetryAttempts} attempts",
            lastException);
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (IsConnected)
            return;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
                return;

            await DisposeConnectionAsync();

            var factory = new ConnectionFactory { Uri = new Uri(_connectionString!) };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.ExchangeDeclareAsync(
                ExchangeName,
                ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation("RabbitMQ event publisher connected");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task PublishBodyAsync(string eventName, byte[] body, CancellationToken cancellationToken)
    {
        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel is not available");

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Type = eventName
        };

        var routingKey = $"sale.{eventName}";
        await _channel.BasicPublishAsync(ExchangeName, routingKey, false, properties, body, cancellationToken);
        _logger.LogDebug("Published {EventName} to RabbitMQ exchange {Exchange} with routing key {RoutingKey}",
            eventName, ExchangeName, routingKey);
    }

    private async Task DisposeConnectionAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
        _connectionLock.Dispose();
    }
}
