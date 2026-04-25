using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleItemTests
{
    [Fact(DisplayName = "Quantity below four should not receive discount")]
    public void Given_QuantityBelowFour_When_CreatingSaleItem_Then_ShouldNotApplyDiscount()
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", 3, 10m);

        Assert.Equal(0m, item.DiscountPercentage);
        Assert.Equal(0m, item.DiscountAmount);
        Assert.Equal(30m, item.TotalAmount);
    }

    [Fact(DisplayName = "Quantity from four to nine should receive ten percent discount")]
    public void Given_QuantityFromFourToNine_When_CreatingSaleItem_Then_ShouldApplyTenPercentDiscount()
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", 4, 10m);

        Assert.Equal(10m, item.DiscountPercentage);
        Assert.Equal(4m, item.DiscountAmount);
        Assert.Equal(36m, item.TotalAmount);
    }

    [Fact(DisplayName = "Quantity from ten to twenty should receive twenty percent discount")]
    public void Given_QuantityFromTenToTwenty_When_CreatingSaleItem_Then_ShouldApplyTwentyPercentDiscount()
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", 10, 10m);

        Assert.Equal(20m, item.DiscountPercentage);
        Assert.Equal(20m, item.DiscountAmount);
        Assert.Equal(80m, item.TotalAmount);
    }

    [Fact(DisplayName = "Quantity above twenty should not be allowed")]
    public void Given_QuantityAboveTwenty_When_CreatingSaleItem_Then_ShouldThrowException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new SaleItem(Guid.NewGuid(), "Product", 21, 10m));

        Assert.Equal("It is not possible to sell more than 20 identical items", exception.Message);
    }

    [Theory(DisplayName = "Quantity lower than one should not be allowed")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Given_QuantityLowerThanOne_When_CreatingSaleItem_Then_ShouldThrowException(int quantity)
    {
        var exception = Assert.Throws<DomainException>(() =>
            new SaleItem(Guid.NewGuid(), "Product", quantity, 10m));

        Assert.Equal("Sale item quantity must be greater than zero", exception.Message);
    }

    [Theory(DisplayName = "Unit price lower than or equal to zero should not be allowed")]
    [InlineData("0")]
    [InlineData("-1")]
    public void Given_InvalidUnitPrice_When_CreatingSaleItem_Then_ShouldThrowException(string unitPrice)
    {
        var exception = Assert.Throws<DomainException>(() =>
            new SaleItem(Guid.NewGuid(), "Product", 1, decimal.Parse(unitPrice)));

        Assert.Equal("Sale item unit price must be greater than zero", exception.Message);
    }

    [Fact(DisplayName = "Quantity twenty should be allowed with twenty percent discount")]
    public void Given_QuantityTwenty_When_CreatingSaleItem_Then_ShouldApplyTwentyPercentDiscount()
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", 20, 10m);

        Assert.Equal(20m, item.DiscountPercentage);
        Assert.Equal(40m, item.DiscountAmount);
        Assert.Equal(160m, item.TotalAmount);
    }

    [Fact(DisplayName = "Quantity nine should keep ten percent discount")]
    public void Given_QuantityNine_When_CreatingSaleItem_Then_ShouldApplyTenPercentDiscount()
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", 9, 10m);

        Assert.Equal(10m, item.DiscountPercentage);
        Assert.Equal(9m, item.DiscountAmount);
        Assert.Equal(81m, item.TotalAmount);
    }

    [Fact(DisplayName = "Cancelled item should keep calculated monetary values")]
    public void Given_ValidSaleItem_When_Cancelling_Then_ShouldKeepCalculatedAmounts()
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", 4, 10m);

        item.Cancel();

        Assert.True(item.IsCancelled);
        Assert.Equal(10m, item.DiscountPercentage);
        Assert.Equal(4m, item.DiscountAmount);
        Assert.Equal(36m, item.TotalAmount);
    }
}
