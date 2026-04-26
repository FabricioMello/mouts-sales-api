using Ambev.DeveloperEvaluation.Application.Sales.Events.Publishing;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.WebApi.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi;

public class OutboxMessageProcessorTests
{
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IEventNotificationPublisher _publisher = Substitute.For<IEventNotificationPublisher>();
    private readonly ILogger<OutboxMessageProcessor> _logger = Substitute.For<ILogger<OutboxMessageProcessor>>();

    [Fact(DisplayName = "Pending outbox messages should be published and marked as processed")]
    public async Task Given_PendingMessages_When_Processing_Then_ShouldPublishAndMarkAsProcessed()
    {
        var messages = new[]
        {
            CreateMessage("SaleCreatedEvent", "{\"saleNumber\":\"SALE-001\"}"),
            CreateMessage("SaleCancelledEvent", "{\"saleNumber\":\"SALE-002\"}")
        };
        _outboxRepository.GetPendingAsync(20, Arg.Any<CancellationToken>())
            .Returns(messages);
        var processor = CreateProcessor();

        await processor.ProcessPendingMessagesAsync(20);

        foreach (var message in messages)
        {
            await _publisher.Received(1).PublishRawAsync(message.EventName, message.Payload, Arg.Any<CancellationToken>());
            await _outboxRepository.Received(1).MarkAsProcessedAsync(message.Id, Arg.Any<CancellationToken>());
        }
        await _outboxRepository.DidNotReceiveWithAnyArgs().MarkAsFailedAsync(default, default!, default);
    }

    [Fact(DisplayName = "Failed publish should mark message as failed and continue processing")]
    public async Task Given_PublishFailure_When_Processing_Then_ShouldMarkFailedAndContinue()
    {
        var failedMessage = CreateMessage("SaleCreatedEvent", "{\"saleNumber\":\"SALE-001\"}");
        var successfulMessage = CreateMessage("SaleCancelledEvent", "{\"saleNumber\":\"SALE-002\"}");
        _outboxRepository.GetPendingAsync(20, Arg.Any<CancellationToken>())
            .Returns([failedMessage, successfulMessage]);
        _publisher.PublishRawAsync(failedMessage.EventName, failedMessage.Payload, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Broker unavailable")));
        var processor = CreateProcessor();

        await processor.ProcessPendingMessagesAsync(20);

        await _outboxRepository.Received(1).MarkAsFailedAsync(
            failedMessage.Id,
            "Broker unavailable",
            Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).MarkAsProcessedAsync(
            successfulMessage.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Empty outbox batch should not publish or update messages")]
    public async Task Given_EmptyBatch_When_Processing_Then_ShouldDoNothing()
    {
        _outboxRepository.GetPendingAsync(20, Arg.Any<CancellationToken>())
            .Returns([]);
        var processor = CreateProcessor();

        await processor.ProcessPendingMessagesAsync(20);

        await _publisher.DidNotReceiveWithAnyArgs().PublishRawAsync(default!, default!, default);
        await _outboxRepository.DidNotReceiveWithAnyArgs().MarkAsProcessedAsync(default, default);
        await _outboxRepository.DidNotReceiveWithAnyArgs().MarkAsFailedAsync(default, default!, default);
    }

    private OutboxMessageProcessor CreateProcessor()
    {
        return new OutboxMessageProcessor(_outboxRepository, _publisher, _logger);
    }

    private static OutboxMessage CreateMessage(string eventName, string payload)
    {
        return new OutboxMessage(eventName, payload, DateTime.UtcNow)
        {
            Id = Guid.NewGuid()
        };
    }
}
