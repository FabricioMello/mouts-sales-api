using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
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
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelSaleItemHandler> _logger;

    public CancelSaleItemHandler(ISaleRepository saleRepository, IMapper mapper, IMediator mediator, IUnitOfWork unitOfWork, ILogger<CancelSaleItemHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
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

        var cancelledItem = updatedSale.Items.First(i => i.Id == command.ItemId);
        await _mediator.Publish(new SaleItemCancelledEvent(
            updatedSale.Id,
            updatedSale.SaleNumber,
            command.ItemId,
            cancelledItem.ProductName,
            updatedSale.TotalAmount,
            DateTime.UtcNow), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SaleResult>(updatedSale);
    }
}
