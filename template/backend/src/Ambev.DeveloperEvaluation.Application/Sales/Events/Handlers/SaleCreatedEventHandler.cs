using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Publishing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Handlers;

public class SaleCreatedEventHandler : INotificationHandler<SaleCreatedEvent>
{
    private readonly ILogger<SaleCreatedEventHandler> _logger;
    private readonly IEventNotificationPublisher _publisher;

    public SaleCreatedEventHandler(ILogger<SaleCreatedEventHandler> logger, IEventNotificationPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
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

        await _publisher.PublishAsync(nameof(SaleCreatedEvent), notification, cancellationToken);
    }
}
