using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class GetSaleHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ILogger<GetSaleHandler> _logger = Substitute.For<ILogger<GetSaleHandler>>();

    private GetSaleHandler CreateHandler() => new(_repository, _mapper, _logger);

    [Fact(DisplayName = "Get sale handler should return mapped sale result")]
    public async Task Given_ValidSaleId_When_GettingSale_Then_ShouldReturnMappedResult()
    {
        var sale = SaleTestDataBuilder.CreateValidSale();
        var command = new GetSaleCommand(sale.Id);
        var handler = CreateHandler();
        var expectedResult = new SaleResult
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            TotalAmount = sale.TotalAmount
        };

        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _mapper.Map<SaleResult>(sale).Returns(expectedResult);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(expectedResult.Id, result.Id);
        Assert.Equal(expectedResult.SaleNumber, result.SaleNumber);
        Assert.Equal(expectedResult.TotalAmount, result.TotalAmount);
    }

    [Fact(DisplayName = "Get sale handler should throw EntityNotFoundException for non-existent sale")]
    public async Task Given_NonExistentSaleId_When_GettingSale_Then_ShouldThrowEntityNotFoundException()
    {
        var saleId = Guid.NewGuid();
        var command = new GetSaleCommand(saleId);
        var handler = CreateHandler();

        _repository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Sale", exception.EntityName);
        Assert.Equal(saleId, exception.EntityId);
    }
}
