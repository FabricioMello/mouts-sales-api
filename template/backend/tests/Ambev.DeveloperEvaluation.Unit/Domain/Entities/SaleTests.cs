using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Fact(DisplayName = "Cancelled sale should keep calculated monetary values")]
    public void Given_ValidSale_When_Cancelling_Then_ShouldKeepCalculatedAmounts()
    {
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch",
            [new SaleItem(Guid.NewGuid(), "Product", 4, 10m)]);

        sale.Cancel();

        Assert.True(sale.IsCancelled);
        Assert.Equal(36m, sale.TotalAmount);
        Assert.All(sale.Items, item =>
        {
            Assert.True(item.IsCancelled);
            Assert.Equal(36m, item.TotalAmount);
        });
    }

    [Fact(DisplayName = "Cancelling sale item should recalculate sale total from active items")]
    public void Given_ValidSale_When_CancellingItem_Then_ShouldRecalculateTotalFromActiveItems()
    {
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

        sale.CancelItem(itemToCancel.Id);

        Assert.False(sale.IsCancelled);
        Assert.True(itemToCancel.IsCancelled);
        Assert.False(activeItem.IsCancelled);
        Assert.Equal(36m, itemToCancel.TotalAmount);
        Assert.Equal(80m, activeItem.TotalAmount);
        Assert.Equal(80m, sale.TotalAmount);
    }

    [Fact(DisplayName = "Cancelled sale should not allow item cancellation")]
    public void Given_CancelledSale_When_CancellingItem_Then_ShouldThrowException()
    {
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

        var exception = Assert.Throws<BusinessRuleViolationException>(() => sale.CancelItem(item.Id));

        Assert.Equal("Cancelled sales cannot be modified", exception.Message);
    }

    [Fact(DisplayName = "Cancelling already cancelled sale should throw business rule violation")]
    public void Given_CancelledSale_When_CancellingAgain_Then_ShouldThrowException()
    {
        var sale = CreateValidSale();
        sale.Cancel();

        var exception = Assert.Throws<BusinessRuleViolationException>(() => sale.Cancel());

        Assert.Equal("Sale is already cancelled", exception.Message);
    }

    [Fact(DisplayName = "Sale should not allow duplicated products")]
    public void Given_DuplicatedProductItems_When_CreatingSale_Then_ShouldThrowException()
    {
        var productId = Guid.NewGuid();

        var exception = Assert.Throws<DomainException>(() =>
            new Sale(
                "SALE-001",
                DateTime.UtcNow,
                Guid.NewGuid(),
                "Customer",
                Guid.NewGuid(),
                "Branch",
                [
                    new SaleItem(productId, "Product", 10, 10m),
                    new SaleItem(productId, "Product", 10, 10m)
                ]));

        Assert.Equal("Sale cannot contain duplicated products", exception.Message);
    }

    [Fact(DisplayName = "Sale with no items should not be allowed")]
    public void Given_NoItems_When_CreatingSale_Then_ShouldThrowException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new Sale(
                "SALE-001",
                DateTime.UtcNow,
                Guid.NewGuid(),
                "Customer",
                Guid.NewGuid(),
                "Branch",
                []));

        Assert.Equal("Sale must have at least one item", exception.Message);
    }

    [Fact(DisplayName = "Sale should set created date when created")]
    public void Given_ValidSale_When_Creating_Then_ShouldSetCreatedAt()
    {
        var sale = CreateValidSale();

        Assert.NotEqual(default, sale.CreatedAt);
        Assert.Null(sale.UpdatedAt);
    }

    [Fact(DisplayName = "Cancelling unknown item should throw entity not found")]
    public void Given_UnknownItem_When_CancellingItem_Then_ShouldThrowException()
    {
        var itemId = Guid.NewGuid();
        var sale = CreateValidSale();

        var exception = Assert.Throws<EntityNotFoundException>(() => sale.CancelItem(itemId));

        Assert.Equal("SaleItem", exception.EntityName);
        Assert.Equal(itemId, exception.EntityId);
    }

    [Fact(DisplayName = "Cancelling already cancelled item should throw business rule violation")]
    public void Given_AlreadyCancelledItem_When_CancellingItem_Then_ShouldThrowException()
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", 4, 10m) { Id = Guid.NewGuid() };
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch",
            [item]);
        sale.CancelItem(item.Id);

        var exception = Assert.Throws<BusinessRuleViolationException>(() => sale.CancelItem(item.Id));

        Assert.Equal("Sale item is already cancelled", exception.Message);
    }

    private static Sale CreateValidSale()
    {
        return new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch",
            [new SaleItem(Guid.NewGuid(), "Product", 4, 10m)]);
    }
}
