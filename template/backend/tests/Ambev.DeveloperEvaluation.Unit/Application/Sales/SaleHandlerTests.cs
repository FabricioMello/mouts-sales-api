using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.Events.Notifications;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
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
        var logger = new ListLogger<CreateSaleHandler>();
        var handler = new CreateSaleHandler(repository, mapper, mediator, logger);
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
        await mediator.Received(1).Publish(Arg.Is<SaleCreatedEvent>(e =>
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
        var logger = new ListLogger<CancelSaleItemHandler>();
        var handler = new CancelSaleItemHandler(repository, mapper, mediator, logger);
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
