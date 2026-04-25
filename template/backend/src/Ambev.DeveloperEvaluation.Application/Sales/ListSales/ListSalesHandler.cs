using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesHandler : IRequestHandler<ListSalesCommand, PagedResult<SaleResult>>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<SaleResult>> Handle(ListSalesCommand command, CancellationToken cancellationToken)
    {
        var validator = new ListSalesValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

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

        var (sales, totalCount) = await _saleRepository.ListAsync(command.Page, command.Size, filter, cancellationToken);

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
