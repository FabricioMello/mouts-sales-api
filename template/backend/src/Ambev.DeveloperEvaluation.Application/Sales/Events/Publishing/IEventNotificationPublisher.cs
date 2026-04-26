namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Publishing;

public interface IEventNotificationPublisher
{
    Task PublishAsync<T>(string eventName, T message, CancellationToken cancellationToken = default) where T : class;
    Task PublishRawAsync(string eventName, string jsonPayload, CancellationToken cancellationToken = default);
}
