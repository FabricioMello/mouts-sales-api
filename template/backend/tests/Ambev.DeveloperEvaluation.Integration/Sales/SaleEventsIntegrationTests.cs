using System.Text;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public class SaleEventsIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .Build();

    public async Task InitializeAsync() => await _rabbitMq.StartAsync();
    public async Task DisposeAsync() => await _rabbitMq.DisposeAsync();

    private RabbitMqEventPublisher CreatePublisher()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:ConnectionString"] = _rabbitMq.GetConnectionString()
            })
            .Build();

        return new RabbitMqEventPublisher(config, Substitute.For<ILogger<RabbitMqEventPublisher>>());
    }

    private async Task<(IConnection Connection, IChannel Channel, string QueueName)> CreateConsumerAsync(string routingKey)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitMq.GetConnectionString()) };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync("sales.events", ExchangeType.Topic, durable: true);
        var queue = await channel.QueueDeclareAsync(exclusive: true);
        await channel.QueueBindAsync(queue.QueueName, "sales.events", routingKey);

        return (connection, channel, queue.QueueName);
    }

    private static async Task<BasicGetResult?> ConsumeOneAsync(IChannel channel, string queueName)
    {
        // Allow time for the message to be routed through the exchange
        await Task.Delay(1000).ConfigureAwait(false);
        return await channel.BasicGetAsync(queueName, autoAck: true).ConfigureAwait(false);
    }

    [Fact(DisplayName = "Creating a sale should publish SaleCreatedEvent to RabbitMQ")]
    public async Task Given_SaleCreated_When_Published_Then_ShouldBeConsumedFromRabbitMQ()
    {
        await using var publisher = CreatePublisher();
        var (connection, channel, queueName) = await CreateConsumerAsync("sale.SaleCreatedEvent");

        var saleEvent = new SaleCreatedEvent(
            Guid.NewGuid(), "SALE-INT-001", Guid.NewGuid(), "Test Customer",
            Guid.NewGuid(), "Test Branch", 360m, 2, DateTime.UtcNow);

        await publisher.PublishAsync(nameof(SaleCreatedEvent), saleEvent);

        var result = await ConsumeOneAsync(channel, queueName);

        Assert.NotNull(result);
        var body = Encoding.UTF8.GetString(result.Body.ToArray());
        var deserialized = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("SALE-INT-001", deserialized.GetProperty("saleNumber").GetString());
        Assert.Equal(360m, deserialized.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(2, deserialized.GetProperty("itemCount").GetInt32());

        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact(DisplayName = "Cancelling a sale should publish SaleCancelledEvent to RabbitMQ")]
    public async Task Given_SaleCancelled_When_Published_Then_ShouldBeConsumedFromRabbitMQ()
    {
        await using var publisher = CreatePublisher();
        var (connection, channel, queueName) = await CreateConsumerAsync("sale.SaleCancelledEvent");

        var saleId = Guid.NewGuid();
        var cancelEvent = new SaleCancelledEvent(saleId, "SALE-INT-002", DateTime.UtcNow);

        await publisher.PublishAsync(nameof(SaleCancelledEvent), cancelEvent);

        var result = await ConsumeOneAsync(channel, queueName);

        Assert.NotNull(result);
        var body = Encoding.UTF8.GetString(result.Body.ToArray());
        var deserialized = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("SALE-INT-002", deserialized.GetProperty("saleNumber").GetString());
        Assert.Equal(saleId.ToString(), deserialized.GetProperty("saleId").GetString());

        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact(DisplayName = "Cancelling a sale item should publish SaleItemCancelledEvent to RabbitMQ")]
    public async Task Given_SaleItemCancelled_When_Published_Then_ShouldBeConsumedFromRabbitMQ()
    {
        await using var publisher = CreatePublisher();
        var (connection, channel, queueName) = await CreateConsumerAsync("sale.SaleItemCancelledEvent");

        var saleId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var itemEvent = new SaleItemCancelledEvent(saleId, "SALE-INT-003", itemId, "Product X", 80m, DateTime.UtcNow);

        await publisher.PublishAsync(nameof(SaleItemCancelledEvent), itemEvent);

        var result = await ConsumeOneAsync(channel, queueName);

        Assert.NotNull(result);
        var body = Encoding.UTF8.GetString(result.Body.ToArray());
        var deserialized = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("SALE-INT-003", deserialized.GetProperty("saleNumber").GetString());
        Assert.Equal("Product X", deserialized.GetProperty("productName").GetString());
        Assert.Equal(80m, deserialized.GetProperty("newSaleTotal").GetDecimal());
        Assert.Equal(itemId.ToString(), deserialized.GetProperty("itemId").GetString());

        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }
}
