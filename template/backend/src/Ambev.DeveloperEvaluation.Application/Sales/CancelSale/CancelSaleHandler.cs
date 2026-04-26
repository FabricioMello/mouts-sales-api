using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
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
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelSaleHandler> _logger;

    public CancelSaleHandler(ISaleRepository saleRepository, IMapper mapper, IMediator mediator, IUnitOfWork unitOfWork, ILogger<CancelSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
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

        await _mediator.Publish(new SaleCancelledEvent(
            cancelledSale.Id,
            cancelledSale.SaleNumber,
            DateTime.UtcNow), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SaleResult>(cancelledSale);
    }
}
