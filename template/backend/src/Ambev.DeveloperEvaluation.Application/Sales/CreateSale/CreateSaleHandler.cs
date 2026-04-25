using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateSaleHandler> _logger;

    public CreateSaleHandler(ISaleRepository saleRepository, IMapper mapper, ILogger<CreateSaleHandler> logger)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating sale {SaleNumber} for customer {CustomerId} at branch {BranchId} with {ItemCount} items",
            command.SaleNumber,
            command.CustomerId,
            command.BranchId,
            command.Items.Count);

        var existingSale = await _saleRepository.GetBySaleNumberAsync(command.SaleNumber, cancellationToken);
        if (existingSale is not null)
            throw new BusinessRuleViolationException($"Sale with number {command.SaleNumber} already exists");

        var sale = new Sale(
            command.SaleNumber,
            command.SaleDate,
            command.CustomerId,
            command.CustomerName,
            command.BranchId,
            command.BranchName,
            command.Items.Select(item => new SaleItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice)));

        var createdSale = await _saleRepository.CreateAsync(sale, cancellationToken);
        _logger.LogInformation(
            "Event {EventName}: sale {SaleId} ({SaleNumber}) created with total {TotalAmount}",
            "SaleCreated",
            createdSale.Id,
            createdSale.SaleNumber,
            createdSale.TotalAmount);

        return _mapper.Map<SaleResult>(createdSale);
    }
}
