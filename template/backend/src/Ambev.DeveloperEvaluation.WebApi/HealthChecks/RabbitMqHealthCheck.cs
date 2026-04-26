using Ambev.DeveloperEvaluation.WebApi.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ambev.DeveloperEvaluation.WebApi.HealthChecks;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqEventPublisher _publisher;

    public RabbitMqHealthCheck(RabbitMqEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isConnected = await _publisher.CheckConnectionAsync(cancellationToken);

        return isConnected
            ? HealthCheckResult.Healthy("RabbitMQ connection is active")
            : HealthCheckResult.Unhealthy("RabbitMQ connection is not available");
    }
}
