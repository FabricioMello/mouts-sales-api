using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;

public record SaleItemCancelledEvent(
    Guid SaleId,
    string SaleNumber,
    Guid ItemId,
    string ProductName,
    decimal NewSaleTotal,
    DateTime OccurredAt) : INotification;
