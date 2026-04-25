using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Publishing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Handlers;

public class SaleItemCancelledEventHandler : INotificationHandler<SaleItemCancelledEvent>
{
    private readonly ILogger<SaleItemCancelledEventHandler> _logger;
    private readonly IEventNotificationPublisher _publisher;

    public SaleItemCancelledEventHandler(ILogger<SaleItemCancelledEventHandler> logger, IEventNotificationPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
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

        await _publisher.PublishAsync(nameof(SaleItemCancelledEvent), notification, cancellationToken);
    }
}
