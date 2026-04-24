using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesCommand : IRequest<PagedResult<SaleResult>>
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
}
