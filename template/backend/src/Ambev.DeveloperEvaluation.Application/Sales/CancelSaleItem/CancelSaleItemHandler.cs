using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

public class CancelSaleItemHandler : IRequestHandler<CancelSaleItemCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CancelSaleItemHandler> _logger;

    public CancelSaleItemHandler(ISaleRepository saleRepository, IMapper mapper, ILogger<CancelSaleItemHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResult> Handle(CancelSaleItemCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling item {ItemId} from sale {SaleId}", command.ItemId, command.SaleId);

        var sale = await _saleRepository.GetByIdAsync(command.SaleId, cancellationToken);
        if (sale is null)
            throw new EntityNotFoundException("Sale", command.SaleId);

        sale.CancelItem(command.ItemId);
        var updatedSale = await _saleRepository.UpdateAsync(sale, cancellationToken);
        _logger.LogInformation(
            "Event {EventName}: item {ItemId} from sale {SaleId} ({SaleNumber}) cancelled. New sale total is {TotalAmount}",
            "SaleItemCancelled",
            command.ItemId,
            updatedSale.Id,
            updatedSale.SaleNumber,
            updatedSale.TotalAmount);

        return _mapper.Map<SaleResult>(updatedSale);
    }
}
