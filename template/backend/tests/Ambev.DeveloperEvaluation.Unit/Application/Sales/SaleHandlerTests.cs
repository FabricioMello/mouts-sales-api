using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class SaleHandlerTests
{
    [Fact(DisplayName = "Create sale handler should persist sale and publish SaleCreatedEvent")]
    public async Task Given_ValidCommand_When_CreatingSale_Then_ShouldPersistAndPublishEvent()
    {
        var repository = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = new ListLogger<CreateSaleHandler>();
        var handler = new CreateSaleHandler(repository, mapper, mediator, unitOfWork, logger);
        var command = CreateValidCreateSaleCommand();

        repository.GetBySaleNumberAsync(command.SaleNumber, Arg.Any<CancellationToken>())
            .Returns((Sale?)null);
        repository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Sale>());
        mapper.Map<SaleResult>(Arg.Any<Sale>())
            .Returns(call =>
            {
                var sale = call.Arg<Sale>();
                return new SaleResult { Id = sale.Id, SaleNumber = sale.SaleNumber, TotalAmount = sale.TotalAmount };
            });

        await handler.Handle(command, CancellationToken.None);

        await repository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await mediator.Received(1).Publish(Arg.Is<SaleCreatedEvent>(e =>
            e.SaleId != Guid.Empty &&
            e.SaleNumber == command.SaleNumber &&
            e.CustomerId == command.CustomerId &&
            e.BranchId == command.BranchId &&
            e.ItemCount == command.Items.Count), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Cancel sale item handler should update sale and publish SaleItemCancelledEvent")]
    public async Task Given_ValidCommand_When_CancellingSaleItem_Then_ShouldUpdateAndPublishEvent()
    {
        var repository = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = new ListLogger<CancelSaleItemHandler>();
        var handler = new CancelSaleItemHandler(repository, mapper, mediator, unitOfWork, logger);
        var itemToCancel = new SaleItem(Guid.NewGuid(), "Product 1", 4, 10m) { Id = Guid.NewGuid() };
        var activeItem = new SaleItem(Guid.NewGuid(), "Product 2", 10, 10m) { Id = Guid.NewGuid() };
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch",
            [itemToCancel, activeItem]);
        var command = new CancelSaleItemCommand(sale.Id, itemToCancel.Id);

        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        repository.UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Sale>());
        mapper.Map<SaleResult>(Arg.Any<Sale>())
            .Returns(call =>
            {
                var updatedSale = call.Arg<Sale>();
                return new SaleResult { Id = updatedSale.Id, SaleNumber = updatedSale.SaleNumber, TotalAmount = updatedSale.TotalAmount };
            });

        await handler.Handle(command, CancellationToken.None);

        await repository.Received(1).UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal(80m, sale.TotalAmount);
        await mediator.Received(1).Publish(Arg.Is<SaleItemCancelledEvent>(e =>
            e.SaleId == sale.Id &&
            e.ItemId == itemToCancel.Id &&
            e.ProductName == "Product 1" &&
            e.NewSaleTotal == 80m), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "List sales handler should normalize date filters to UTC")]
    public async Task Given_UnspecifiedDateFilters_When_ListingSales_Then_ShouldNormalizeDatesToUtc()
    {
        var repository = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = new ListLogger<ListSalesHandler>();
        var handler = new ListSalesHandler(repository, mapper, logger);
        SaleFilter? capturedFilter = null;
        var command = new ListSalesCommand
        {
            Page = 1,
            Size = 10,
            SaleDateFrom = new DateTime(2026, 1, 1),
            SaleDateTo = new DateTime(2026, 1, 31)
        };

        repository
            .ListAsync(command.Page, command.Size, Arg.Do<SaleFilter>(filter => capturedFilter = filter), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Sale>(), 0));
        mapper.Map<IReadOnlyList<SaleResult>>(Arg.Any<IReadOnlyList<Sale>>()).Returns([]);

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedFilter);
        Assert.Equal(DateTimeKind.Utc, capturedFilter!.SaleDateFrom!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, capturedFilter.SaleDateTo!.Value.Kind);
    }

    [Fact(DisplayName = "Create sale handler should throw BusinessRuleViolationException for duplicate sale number")]
    public async Task Given_DuplicateSaleNumber_When_CreatingSale_Then_ShouldThrowBusinessRuleViolation()
    {
        var repository = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = new ListLogger<CreateSaleHandler>();
        var handler = new CreateSaleHandler(repository, mapper, mediator, unitOfWork, logger);
        var command = CreateValidCreateSaleCommand();
        var existingSale = new Sale(
            command.SaleNumber,
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Existing Customer",
            Guid.NewGuid(),
            "Existing Branch",
            [new SaleItem(Guid.NewGuid(), "Product", 1, 10m)]);

        repository.GetBySaleNumberAsync(command.SaleNumber, Arg.Any<CancellationToken>())
            .Returns(existingSale);

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("already exists", exception.Message);
        await repository.DidNotReceive().CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Cancel sale item handler should throw EntityNotFoundException for non-existent sale")]
    public async Task Given_NonExistentSale_When_CancellingSaleItem_Then_ShouldThrowEntityNotFoundException()
    {
        var repository = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = new ListLogger<CancelSaleItemHandler>();
        var handler = new CancelSaleItemHandler(repository, mapper, mediator, unitOfWork, logger);
        var saleId = Guid.NewGuid();
        var command = new CancelSaleItemCommand(saleId, Guid.NewGuid());

        repository.GetByIdAsync(saleId, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Sale", exception.EntityName);
    }

    [Fact(DisplayName = "Cancel sale item handler should throw BusinessRuleViolationException for cancelled sale")]
    public async Task Given_CancelledSale_When_CancellingSaleItem_Then_ShouldThrowBusinessRuleViolation()
    {
        var repository = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = new ListLogger<CancelSaleItemHandler>();
        var handler = new CancelSaleItemHandler(repository, mapper, mediator, unitOfWork, logger);
        var item = new SaleItem(Guid.NewGuid(), "Product", 4, 10m) { Id = Guid.NewGuid() };
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch",
            [item]);
        sale.Cancel();
        var command = new CancelSaleItemCommand(sale.Id, item.Id);

        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Cancelled sales cannot be modified", exception.Message);
    }

    [Fact(DisplayName = "Cancel sale item handler should throw EntityNotFoundException for non-existent item")]
    public async Task Given_NonExistentItem_When_CancellingSaleItem_Then_ShouldThrowEntityNotFoundException()
    {
        var repository = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = new ListLogger<CancelSaleItemHandler>();
        var handler = new CancelSaleItemHandler(repository, mapper, mediator, unitOfWork, logger);
        var item = new SaleItem(Guid.NewGuid(), "Product", 4, 10m) { Id = Guid.NewGuid() };
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch",
            [item]);
        var unknownItemId = Guid.NewGuid();
        var command = new CancelSaleItemCommand(sale.Id, unknownItemId);

        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("SaleItem", exception.EntityName);
        Assert.Equal(unknownItemId, exception.EntityId);
    }

    [Fact(DisplayName = "Cancel sale item handler should throw BusinessRuleViolationException for already cancelled item")]
    public async Task Given_AlreadyCancelledItem_When_CancellingSaleItem_Then_ShouldThrowBusinessRuleViolation()
    {
        var repository = Substitute.For<ISaleRepository>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = new ListLogger<CancelSaleItemHandler>();
        var handler = new CancelSaleItemHandler(repository, mapper, mediator, unitOfWork, logger);
        var item1 = new SaleItem(Guid.NewGuid(), "Product 1", 4, 10m) { Id = Guid.NewGuid() };
        var item2 = new SaleItem(Guid.NewGuid(), "Product 2", 4, 10m) { Id = Guid.NewGuid() };
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch",
            [item1, item2]);
        sale.CancelItem(item1.Id);
        var command = new CancelSaleItemCommand(sale.Id, item1.Id);

        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Sale item is already cancelled", exception.Message);
    }

    private static CreateSaleCommand CreateValidCreateSaleCommand()
    {
        return new CreateSaleCommand
        {
            SaleNumber = "SALE-001",
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Customer",
            BranchId = Guid.NewGuid(),
            BranchName = "Branch",
            Items =
            [
                new SaleItemCommand
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product",
                    Quantity = 4,
                    UnitPrice = 10m
                }
            ]
        };
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
