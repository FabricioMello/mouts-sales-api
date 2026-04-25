using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSale;

public class CancelSaleHandler : IRequestHandler<CancelSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CancelSaleHandler> _logger;

    public CancelSaleHandler(ISaleRepository saleRepository, IMapper mapper, ILogger<CancelSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResult> Handle(CancelSaleCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling sale {SaleId}", command.Id);

        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (sale is null)
            throw new EntityNotFoundException("Sale", command.Id);

        sale.Cancel();
        var cancelledSale = await _saleRepository.UpdateAsync(sale, cancellationToken);
        _logger.LogInformation(
            "Event {EventName}: sale {SaleId} ({SaleNumber}) cancelled",
            "SaleCancelled",
            cancelledSale.Id,
            cancelledSale.SaleNumber);

        return _mapper.Map<SaleResult>(cancelledSale);
    }
}
