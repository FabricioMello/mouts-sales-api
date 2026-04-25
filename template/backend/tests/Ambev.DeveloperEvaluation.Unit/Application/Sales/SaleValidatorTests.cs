using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using FluentValidation.TestHelper;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class SaleValidatorTests
{
    [Fact(DisplayName = "Create sale validator should reject duplicated products")]
    public void Given_DuplicatedProducts_When_ValidatingCreateSale_Then_ShouldHaveError()
    {
        var productId = Guid.NewGuid();
        var command = CreateValidCreateSaleCommand(
            new SaleItemCommand { ProductId = productId, ProductName = "Product", Quantity = 10, UnitPrice = 10m },
            new SaleItemCommand { ProductId = productId, ProductName = "Product", Quantity = 5, UnitPrice = 10m });
        var validator = new CreateSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.Items)
            .WithErrorMessage("Sale cannot contain duplicated products");
    }

    [Fact(DisplayName = "Create sale validator should accept distinct products")]
    public void Given_DistinctProducts_When_ValidatingCreateSale_Then_ShouldNotHaveDuplicatedProductError()
    {
        var command = CreateValidCreateSaleCommand(
            new SaleItemCommand { ProductId = Guid.NewGuid(), ProductName = "Product 1", Quantity = 10, UnitPrice = 10m },
            new SaleItemCommand { ProductId = Guid.NewGuid(), ProductName = "Product 2", Quantity = 5, UnitPrice = 10m });
        var validator = new CreateSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(sale => sale.Items);
    }

    private static CreateSaleCommand CreateValidCreateSaleCommand(params SaleItemCommand[] items)
    {
        return new CreateSaleCommand
        {
            SaleNumber = "SALE-001",
            SaleDate = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            CustomerName = "Customer",
            BranchId = Guid.NewGuid(),
            BranchName = "Branch",
            Items = items.ToList()
        };
    }
}
