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

    [Fact(DisplayName = "Sale update should not allow duplicated products")]
    public void Given_DuplicatedProductItems_When_UpdatingSale_Then_ShouldThrowException()
    {
        var sale = new Sale(
            "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch",
            [new SaleItem(Guid.NewGuid(), "Product", 4, 10m)]);
        var productId = Guid.NewGuid();

        var exception = Assert.Throws<DomainException>(() =>
            sale.UpdateDetails(
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
}
