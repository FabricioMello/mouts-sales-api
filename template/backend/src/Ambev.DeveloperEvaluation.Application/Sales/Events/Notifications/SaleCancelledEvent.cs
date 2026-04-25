using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;

public record SaleCancelledEvent(
    Guid SaleId,
    string SaleNumber,
    DateTime OccurredAt) : INotification;
