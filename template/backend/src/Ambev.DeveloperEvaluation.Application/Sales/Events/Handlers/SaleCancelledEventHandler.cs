using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Publishing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Handlers;

public class SaleCancelledEventHandler : INotificationHandler<SaleCancelledEvent>
{
    private readonly ILogger<SaleCancelledEventHandler> _logger;
    private readonly IEventNotificationPublisher _publisher;

    public SaleCancelledEventHandler(ILogger<SaleCancelledEventHandler> logger, IEventNotificationPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(SaleCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Event {EventName}: sale {SaleId} ({SaleNumber}) cancelled",
            nameof(SaleCancelledEvent),
            notification.SaleId,
            notification.SaleNumber);

        await _publisher.PublishAsync(nameof(SaleCancelledEvent), notification, cancellationToken);
    }
}
