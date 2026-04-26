using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Integration.Fixtures;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public class SaleRepositoryIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public SaleRepositoryIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "CreateAsync should persist sale with items and calculated totals")]
    public async Task Given_SaleWithItems_When_Created_Then_ShouldPersistCalculatedTotals()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SaleRepository(context);
        var sale = CreateSale("SALE-REPO-CREATE", items:
        [
            CreateItem("Product 1", 10, 10m),
            CreateItem("Product 2", 4, 10m)
        ]);

        await repository.CreateAsync(sale);
        await context.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sale.Id);

        Assert.NotNull(persisted);
        Assert.Equal(2, persisted.Items.Count);
        Assert.Equal(116m, persisted.TotalAmount);
        Assert.Contains(persisted.Items, item => item.ProductName == "Product 1" && item.DiscountPercentage == 20m);
        Assert.Contains(persisted.Items, item => item.ProductName == "Product 2" && item.DiscountPercentage == 10m);
    }

    [Fact(DisplayName = "GetByIdAsync should return sale with items")]
    public async Task Given_ExistingSale_When_GetById_Then_ShouldReturnSaleWithItems()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SaleRepository(context);
        var sale = await PersistSaleAsync(context, "SALE-REPO-BY-ID");

        var result = await repository.GetByIdAsync(sale.Id);

        Assert.NotNull(result);
        Assert.Equal(sale.Id, result.Id);
        Assert.NotEmpty(result.Items);
    }

    [Fact(DisplayName = "GetBySaleNumberAsync should return sale by sale number")]
    public async Task Given_ExistingSale_When_GetBySaleNumber_Then_ShouldReturnSale()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SaleRepository(context);
        await PersistSaleAsync(context, "SALE-REPO-BY-NUMBER");

        var result = await repository.GetBySaleNumberAsync("SALE-REPO-BY-NUMBER");

        Assert.NotNull(result);
        Assert.Equal("SALE-REPO-BY-NUMBER", result.SaleNumber);
        Assert.NotEmpty(result.Items);
    }

    [Fact(DisplayName = "GetByIdAsync should return null when sale does not exist")]
    public async Task Given_UnknownSaleId_When_GetById_Then_ShouldReturnNull()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SaleRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact(DisplayName = "ListAsync should filter sales by customer id")]
    public async Task Given_CustomerFilter_When_List_Then_ShouldReturnOnlyCustomerSales()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SaleRepository(context);
        var customerId = Guid.NewGuid();

        var expected1 = await PersistSaleAsync(context, "SALE-REPO-CUSTOMER-1", customerId: customerId);
        var expected2 = await PersistSaleAsync(context, "SALE-REPO-CUSTOMER-2", customerId: customerId);
        await PersistSaleAsync(context, "SALE-REPO-CUSTOMER-OTHER", customerId: Guid.NewGuid());

        var result = await repository.ListAsync(1, 10, new SaleFilter { CustomerId = customerId });

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, sale => sale.Id == expected1.Id);
        Assert.Contains(result.Items, sale => sale.Id == expected2.Id);
        Assert.All(result.Items, sale => Assert.Equal(customerId, sale.CustomerId));
    }

    [Fact(DisplayName = "ListAsync should paginate sales")]
    public async Task Given_Pagination_When_List_Then_ShouldReturnExpectedPage()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SaleRepository(context);
        var branchId = Guid.NewGuid();
        var baseDate = DateTime.UtcNow.Date;

        for (var index = 1; index <= 5; index++)
        {
            await PersistSaleAsync(
                context,
                $"SALE-REPO-PAGE-{index}",
                branchId: branchId,
                saleDate: baseDate.AddDays(index));
        }

        var result = await repository.ListAsync(2, 2, new SaleFilter { BranchId = branchId });

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(["SALE-REPO-PAGE-3", "SALE-REPO-PAGE-2"], result.Items.Select(sale => sale.SaleNumber));
    }

    [Fact(DisplayName = "ListAsync should filter sales by date range")]
    public async Task Given_DateRangeFilter_When_List_Then_ShouldReturnSalesInsideRange()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SaleRepository(context);
        var branchId = Guid.NewGuid();
        var baseDate = DateTime.UtcNow.Date;

        await PersistSaleAsync(context, "SALE-REPO-DATE-OLD", branchId: branchId, saleDate: baseDate.AddDays(-2));
        var expected = await PersistSaleAsync(context, "SALE-REPO-DATE-MID", branchId: branchId, saleDate: baseDate);
        await PersistSaleAsync(context, "SALE-REPO-DATE-NEW", branchId: branchId, saleDate: baseDate.AddDays(2));

        var result = await repository.ListAsync(1, 10, new SaleFilter
        {
            BranchId = branchId,
            SaleDateFrom = baseDate.AddHours(-1),
            SaleDateTo = baseDate.AddHours(1)
        });

        Assert.Single(result.Items);
        Assert.Equal(expected.Id, result.Items[0].Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact(DisplayName = "UpdateAsync should persist sale cancellation")]
    public async Task Given_SaleCancellation_When_Updated_Then_ShouldPersistChanges()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SaleRepository(context);
        var sale = await PersistSaleAsync(context, "SALE-REPO-UPDATE");

        sale.Cancel();
        await repository.UpdateAsync(sale);
        await context.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.Sales
            .Include(s => s.Items)
            .FirstAsync(s => s.Id == sale.Id);

        Assert.True(updated.IsCancelled);
        Assert.All(updated.Items, item => Assert.True(item.IsCancelled));
        Assert.Equal(sale.TotalAmount, updated.TotalAmount);
    }

    [Fact(DisplayName = "CreateAsync should fail when sale number already exists")]
    public async Task Given_DuplicateSaleNumber_When_Created_Then_ShouldThrowDbUpdateException()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = new SaleRepository(context);
        await PersistSaleAsync(context, "SALE-REPO-DUPLICATE");

        var duplicate = CreateSale("SALE-REPO-DUPLICATE");
        await repository.CreateAsync(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task<Sale> PersistSaleAsync(
        DbContext context,
        string saleNumber,
        Guid? customerId = null,
        Guid? branchId = null,
        DateTime? saleDate = null)
    {
        var sale = CreateSale(saleNumber, customerId, branchId, saleDate);

        await context.Set<Sale>().AddAsync(sale);
        await context.SaveChangesAsync();

        return sale;
    }

    private static Sale CreateSale(
        string saleNumber,
        Guid? customerId = null,
        Guid? branchId = null,
        DateTime? saleDate = null,
        IReadOnlyList<SaleItem>? items = null)
    {
        var sale = new Sale(
            saleNumber,
            saleDate ?? DateTime.UtcNow,
            customerId ?? Guid.NewGuid(),
            "Repository Customer",
            branchId ?? Guid.NewGuid(),
            "Repository Branch",
            items ?? [CreateItem("Repository Product", 5, 20m)])
        {
            Id = Guid.NewGuid()
        };

        return sale;
    }

    private static SaleItem CreateItem(string productName, int quantity, decimal unitPrice)
    {
        return new SaleItem(Guid.NewGuid(), productName, quantity, unitPrice)
        {
            Id = Guid.NewGuid()
        };
    }
}
