using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Serialization;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Handlers;

public class SaleCancelledEventHandler : INotificationHandler<SaleCancelledEvent>
{
    private readonly ILogger<SaleCancelledEventHandler> _logger;
    private readonly IOutboxRepository _outboxRepository;

    public SaleCancelledEventHandler(ILogger<SaleCancelledEventHandler> logger, IOutboxRepository outboxRepository)
    {
        _logger = logger;
        _outboxRepository = outboxRepository;
    }

    public async Task Handle(SaleCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Event {EventName}: sale {SaleId} ({SaleNumber}) cancelled",
            nameof(SaleCancelledEvent),
            notification.SaleId,
            notification.SaleNumber);

        var payload = JsonSerializer.Serialize(notification, SalesEventJsonSerializerOptions.Default);
        var outboxMessage = new OutboxMessage(nameof(SaleCancelledEvent), payload, notification.OccurredAt);
        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
    }
}
