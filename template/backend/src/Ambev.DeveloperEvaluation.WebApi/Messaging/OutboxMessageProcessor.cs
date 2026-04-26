using Ambev.DeveloperEvaluation.Application.Sales.Events.Publishing;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.WebApi.Messaging;

public class OutboxMessageProcessor
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventNotificationPublisher _publisher;
    private readonly ILogger<OutboxMessageProcessor> _logger;

    public OutboxMessageProcessor(
        IOutboxRepository outboxRepository,
        IEventNotificationPublisher publisher,
        ILogger<OutboxMessageProcessor> logger)
    {
        _outboxRepository = outboxRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ProcessPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var messages = await _outboxRepository.GetPendingAsync(batchSize, cancellationToken);
        if (messages.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} pending outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await _publisher.PublishRawAsync(message.EventName, message.Payload, cancellationToken);
                await _outboxRepository.MarkAsProcessedAsync(message.Id, cancellationToken);

                _logger.LogDebug(
                    "Outbox message {MessageId} ({EventName}) published successfully",
                    message.Id,
                    message.EventName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to publish outbox message {MessageId} ({EventName}), retry {RetryCount}",
                    message.Id,
                    message.EventName,
                    message.RetryCount + 1);

                await _outboxRepository.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }
}
