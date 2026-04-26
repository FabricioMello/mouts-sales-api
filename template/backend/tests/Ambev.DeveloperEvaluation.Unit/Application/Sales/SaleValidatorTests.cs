using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
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

    [Fact(DisplayName = "Create sale validator should reject sale number exceeding max length")]
    public void Given_SaleNumberExceedingMaxLength_When_ValidatingCreateSale_Then_ShouldHaveError()
    {
        var command = CreateValidCreateSaleCommand(
            new SaleItemCommand { ProductId = Guid.NewGuid(), ProductName = "Product", Quantity = 1, UnitPrice = 10m });
        command.SaleNumber = new string('A', 51);
        var validator = new CreateSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.SaleNumber);
    }

    [Fact(DisplayName = "Create sale validator should reject customer name exceeding max length")]
    public void Given_CustomerNameExceedingMaxLength_When_ValidatingCreateSale_Then_ShouldHaveError()
    {
        var command = CreateValidCreateSaleCommand(
            new SaleItemCommand { ProductId = Guid.NewGuid(), ProductName = "Product", Quantity = 1, UnitPrice = 10m });
        command.CustomerName = new string('A', 101);
        var validator = new CreateSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.CustomerName);
    }

    [Fact(DisplayName = "Create sale validator should reject branch name exceeding max length")]
    public void Given_BranchNameExceedingMaxLength_When_ValidatingCreateSale_Then_ShouldHaveError()
    {
        var command = CreateValidCreateSaleCommand(
            new SaleItemCommand { ProductId = Guid.NewGuid(), ProductName = "Product", Quantity = 1, UnitPrice = 10m });
        command.BranchName = new string('A', 101);
        var validator = new CreateSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.BranchName);
    }

    [Fact(DisplayName = "Create sale validator should reject empty sale number")]
    public void Given_EmptySaleNumber_When_ValidatingCreateSale_Then_ShouldHaveError()
    {
        var command = CreateValidCreateSaleCommand(
            new SaleItemCommand { ProductId = Guid.NewGuid(), ProductName = "Product", Quantity = 1, UnitPrice = 10m });
        command.SaleNumber = string.Empty;
        var validator = new CreateSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.SaleNumber);
    }

    [Fact(DisplayName = "Create sale validator should reject empty customer id")]
    public void Given_EmptyCustomerId_When_ValidatingCreateSale_Then_ShouldHaveError()
    {
        var command = CreateValidCreateSaleCommand(
            new SaleItemCommand { ProductId = Guid.NewGuid(), ProductName = "Product", Quantity = 1, UnitPrice = 10m });
        command.CustomerId = Guid.Empty;
        var validator = new CreateSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.CustomerId);
    }

    [Fact(DisplayName = "List sales validator should reject page zero")]
    public void Given_PageZero_When_ValidatingListSales_Then_ShouldHaveError()
    {
        var command = new ListSalesCommand { Page = 0, Size = 10 };
        var validator = new ListSalesValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.Page);
    }

    [Fact(DisplayName = "List sales validator should reject size above one hundred")]
    public void Given_SizeAboveOneHundred_When_ValidatingListSales_Then_ShouldHaveError()
    {
        var command = new ListSalesCommand { Page = 1, Size = 101 };
        var validator = new ListSalesValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.Size);
    }

    [Fact(DisplayName = "Cancel sale validator should reject empty id")]
    public void Given_EmptyId_When_ValidatingCancelSale_Then_ShouldHaveError()
    {
        var command = new CancelSaleCommand(Guid.Empty);
        var validator = new CancelSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.Id);
    }

    [Fact(DisplayName = "Cancel sale item validator should reject empty ids")]
    public void Given_EmptyIds_When_ValidatingCancelSaleItem_Then_ShouldHaveError()
    {
        var command = new CancelSaleItemCommand(Guid.Empty, Guid.Empty);
        var validator = new CancelSaleItemValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.SaleId);
        result.ShouldHaveValidationErrorFor(c => c.ItemId);
    }

    [Fact(DisplayName = "Get sale validator should reject empty id")]
    public void Given_EmptyId_When_ValidatingGetSale_Then_ShouldHaveError()
    {
        var command = new GetSaleCommand(Guid.Empty);
        var validator = new GetSaleValidator();

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(sale => sale.Id);
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
