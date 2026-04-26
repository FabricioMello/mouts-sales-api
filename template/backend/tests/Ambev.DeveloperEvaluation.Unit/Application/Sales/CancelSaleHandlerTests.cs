using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CancelSaleHandlerTests
{
    private readonly ISaleRepository _repository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<CancelSaleHandler> _logger = Substitute.For<ILogger<CancelSaleHandler>>();

    private CancelSaleHandler CreateHandler() => new(_repository, _mapper, _mediator, _unitOfWork, _logger);

    [Fact(DisplayName = "Cancel sale handler should persist cancellation and publish SaleCancelledEvent")]
    public async Task Given_ValidSaleId_When_CancellingSale_Then_ShouldUpdateAndPublishEvent()
    {
        var sale = SaleTestDataBuilder.CreateValidSale();
        var command = new CancelSaleCommand(sale.Id);
        var handler = CreateHandler();

        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        _repository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Sale>());
        _mapper.Map<SaleResult>(Arg.Any<Sale>())
            .Returns(new SaleResult { Id = sale.Id, SaleNumber = sale.SaleNumber, IsCancelled = true });

        await handler.Handle(command, CancellationToken.None);

        Assert.True(sale.IsCancelled);
        await _repository.Received(1).UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _mediator.Received(1).Publish(Arg.Is<SaleCancelledEvent>(e =>
            e.SaleId == sale.Id &&
            e.SaleNumber == sale.SaleNumber), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Cancel sale handler should throw EntityNotFoundException for non-existent sale")]
    public async Task Given_NonExistentSaleId_When_CancellingSale_Then_ShouldThrowEntityNotFoundException()
    {
        var saleId = Guid.NewGuid();
        var command = new CancelSaleCommand(saleId);
        var handler = CreateHandler();

        _repository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Sale", exception.EntityName);
        Assert.Equal(saleId, exception.EntityId);
    }

    [Fact(DisplayName = "Cancel sale handler should throw BusinessRuleViolationException for already cancelled sale")]
    public async Task Given_AlreadyCancelledSale_When_CancellingSale_Then_ShouldThrowBusinessRuleViolation()
    {
        var sale = SaleTestDataBuilder.CreateCancelledSale();
        var command = new CancelSaleCommand(sale.Id);
        var handler = CreateHandler();

        _repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Sale is already cancelled", exception.Message);
    }
}
