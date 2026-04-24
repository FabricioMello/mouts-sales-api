using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public record GetSaleCommand(Guid Id) : IRequest<SaleResult>;
