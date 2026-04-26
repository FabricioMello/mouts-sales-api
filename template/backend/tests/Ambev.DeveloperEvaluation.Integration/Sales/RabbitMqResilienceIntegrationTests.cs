using System.Reflection;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.WebApi.HealthChecks;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public class RabbitMqResilienceIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .Build();

    public async Task InitializeAsync() => await _rabbitMq.StartAsync();
    public async Task DisposeAsync() => await _rabbitMq.DisposeAsync();

    private RabbitMqEventPublisher CreatePublisher(string? connectionString = null)
    {
        var values = new Dictionary<string, string?>();
        if (connectionString is not null)
            values["RabbitMq:ConnectionString"] = connectionString;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new RabbitMqEventPublisher(config, Substitute.For<ILogger<RabbitMqEventPublisher>>());
    }

    [Fact(DisplayName = "Publisher should connect lazily when publishing")]
    public async Task Given_RabbitMq_When_Publishing_Then_ShouldConnectLazily()
    {
        await using var publisher = CreatePublisher(_rabbitMq.GetConnectionString());
        var saleEvent = new SaleCancelledEvent(Guid.NewGuid(), "SALE-RES-001", DateTime.UtcNow);

        Assert.False(publisher.IsConnected);

        await publisher.PublishAsync(nameof(SaleCancelledEvent), saleEvent);

        Assert.True(publisher.IsConnected);
    }

    [Fact(DisplayName = "Publisher should reconnect when previous connection was closed")]
    public async Task Given_ClosedConnection_When_PublishingAgain_Then_ShouldReconnect()
    {
        await using var publisher = CreatePublisher(_rabbitMq.GetConnectionString());
        var firstEvent = new SaleCancelledEvent(Guid.NewGuid(), "SALE-RES-002", DateTime.UtcNow);
        var secondEvent = new SaleCancelledEvent(Guid.NewGuid(), "SALE-RES-003", DateTime.UtcNow);

        await publisher.PublishAsync(nameof(SaleCancelledEvent), firstEvent);
        Assert.True(publisher.IsConnected);

        var connection = GetCurrentConnection(publisher);
        await connection.DisposeAsync();
        Assert.False(publisher.IsConnected);

        await publisher.PublishAsync(nameof(SaleCancelledEvent), secondEvent);

        Assert.True(publisher.IsConnected);
    }

    [Fact(DisplayName = "RabbitMQ health check should return healthy when broker is available")]
    public async Task Given_AvailableRabbitMq_When_CheckHealth_Then_ShouldReturnHealthy()
    {
        await using var publisher = CreatePublisher(_rabbitMq.GetConnectionString());
        var healthCheck = new RabbitMqHealthCheck(publisher);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.True(publisher.IsConnected);
    }

    [Fact(DisplayName = "RabbitMQ health check should return unhealthy when connection string is missing")]
    public async Task Given_MissingConnectionString_When_CheckHealth_Then_ShouldReturnUnhealthy()
    {
        await using var publisher = CreatePublisher();
        var healthCheck = new RabbitMqHealthCheck(publisher);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.False(publisher.IsConnected);
    }

    private static IConnection GetCurrentConnection(RabbitMqEventPublisher publisher)
    {
        var field = typeof(RabbitMqEventPublisher).GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic);
        var connection = field?.GetValue(publisher) as IConnection;

        Assert.NotNull(connection);
        return connection;
    }
}
