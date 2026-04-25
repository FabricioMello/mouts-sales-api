using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;

public record SaleCreatedEvent(
    Guid SaleId,
    string SaleNumber,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName,
    decimal TotalAmount,
    int ItemCount,
    DateTime OccurredAt) : INotification;
