using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
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

    [Fact(DisplayName = "Create sale validator should reject empty items")]
    public void Given_EmptyItems_When_ValidatingCreateSale_Then_ShouldHaveError()
    {
        var command = CreateValidCreateSaleCommand();
        var validator = new CreateSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.Items);
    }

    [Fact(DisplayName = "Create sale validator should reject item quantity above twenty")]
    public void Given_ItemQuantityAboveTwenty_When_ValidatingCreateSale_Then_ShouldHaveError()
    {
        var command = CreateValidCreateSaleCommand(
            new SaleItemCommand { ProductId = Guid.NewGuid(), ProductName = "Product", Quantity = 21, UnitPrice = 10m });
        var validator = new CreateSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact(DisplayName = "List sales validator should reject invalid date range")]
    public void Given_InvalidDateRange_When_ValidatingListSales_Then_ShouldHaveError()
    {
        var command = new ListSalesCommand
        {
            Page = 1,
            Size = 10,
            SaleDateFrom = new DateTime(2026, 1, 2),
            SaleDateTo = new DateTime(2026, 1, 1)
        };
        var validator = new ListSalesValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.SaleDateTo)
            .WithErrorMessage("SaleDateTo must be greater than or equal to SaleDateFrom");
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
