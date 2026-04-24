using Ambev.DeveloperEvaluation.Domain.Entities;
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
}
