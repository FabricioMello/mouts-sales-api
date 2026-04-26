using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Serialization;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Handlers;

public class SaleItemCancelledEventHandler : INotificationHandler<SaleItemCancelledEvent>
{
    private readonly ILogger<SaleItemCancelledEventHandler> _logger;
    private readonly IOutboxRepository _outboxRepository;

    public SaleItemCancelledEventHandler(ILogger<SaleItemCancelledEventHandler> logger, IOutboxRepository outboxRepository)
    {
        _logger = logger;
        _outboxRepository = outboxRepository;
    }

    public async Task Handle(SaleItemCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Event {EventName}: item {ItemId} ({ProductName}) from sale {SaleId} ({SaleNumber}) cancelled. New sale total is {NewSaleTotal}",
            nameof(SaleItemCancelledEvent),
            notification.ItemId,
            notification.ProductName,
            notification.SaleId,
            notification.SaleNumber,
            notification.NewSaleTotal);

        var payload = JsonSerializer.Serialize(notification, SalesEventJsonSerializerOptions.Default);
        var outboxMessage = new OutboxMessage(nameof(SaleItemCancelledEvent), payload, notification.OccurredAt);
        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
    }
}
