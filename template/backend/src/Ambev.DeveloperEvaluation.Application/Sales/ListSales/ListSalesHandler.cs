using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesHandler : IRequestHandler<ListSalesCommand, PagedResult<SaleResult>>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ListSalesHandler> _logger;

    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper, ILogger<ListSalesHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<SaleResult>> Handle(ListSalesCommand command, CancellationToken cancellationToken)
    {
        var filter = new SaleFilter
        {
            SaleNumber = command.SaleNumber,
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            BranchId = command.BranchId,
            BranchName = command.BranchName,
            IsCancelled = command.IsCancelled,
            SaleDateFrom = NormalizeDate(command.SaleDateFrom),
            SaleDateTo = NormalizeDate(command.SaleDateTo),
        };

        _logger.LogInformation(
            "Listing sales page {Page} size {Size} with filters SaleNumber={SaleNumber}, CustomerId={CustomerId}, BranchId={BranchId}, IsCancelled={IsCancelled}, SaleDateFrom={SaleDateFrom}, SaleDateTo={SaleDateTo}",
            command.Page,
            command.Size,
            filter.SaleNumber,
            filter.CustomerId,
            filter.BranchId,
            filter.IsCancelled,
            filter.SaleDateFrom,
            filter.SaleDateTo);

        var (sales, totalCount) = await _saleRepository.ListAsync(command.Page, command.Size, filter, cancellationToken);
        _logger.LogInformation("Listed {ReturnedCount} sales from {TotalCount} matching records", sales.Count, totalCount);

        return new PagedResult<SaleResult>
        {
            Items = _mapper.Map<IReadOnlyList<SaleResult>>(sales),
            CurrentPage = command.Page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)command.Size),
            TotalCount = totalCount
        };
    }

    private static DateTime? NormalizeDate(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        return date.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc)
            : date.Value.ToUniversalTime();
    }
}
