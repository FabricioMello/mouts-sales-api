using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Serialization;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Handlers;

public class SaleCreatedEventHandler : INotificationHandler<SaleCreatedEvent>
{
    private readonly ILogger<SaleCreatedEventHandler> _logger;
    private readonly IOutboxRepository _outboxRepository;

    public SaleCreatedEventHandler(ILogger<SaleCreatedEventHandler> logger, IOutboxRepository outboxRepository)
    {
        _logger = logger;
        _outboxRepository = outboxRepository;
    }

    public async Task Handle(SaleCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Event {EventName}: sale {SaleId} ({SaleNumber}) created for customer {CustomerId} at branch {BranchId} with {ItemCount} items totalling {TotalAmount}",
            nameof(SaleCreatedEvent),
            notification.SaleId,
            notification.SaleNumber,
            notification.CustomerId,
            notification.BranchId,
            notification.ItemCount,
            notification.TotalAmount);

        var payload = JsonSerializer.Serialize(notification, SalesEventJsonSerializerOptions.Default);
        var outboxMessage = new OutboxMessage(nameof(SaleCreatedEvent), payload, notification.OccurredAt);
        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
    }
}
