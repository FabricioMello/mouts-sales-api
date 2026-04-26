using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales.TestData;

public static class SaleTestDataBuilder
{
    private static readonly Faker<SaleItemCommand> SaleItemCommandFaker = new Faker<SaleItemCommand>()
        .RuleFor(i => i.ProductId, f => f.Random.Guid())
        .RuleFor(i => i.ProductName, f => f.Commerce.ProductName())
        .RuleFor(i => i.Quantity, f => f.Random.Int(1, 20))
        .RuleFor(i => i.UnitPrice, f => f.Finance.Amount(1m, 500m));

    private static readonly Faker<CreateSaleCommand> CreateSaleCommandFaker = new Faker<CreateSaleCommand>()
        .RuleFor(s => s.SaleNumber, f => $"SALE-{f.Random.AlphaNumeric(8).ToUpper()}")
        .RuleFor(s => s.SaleDate, f => f.Date.Recent().ToUniversalTime())
        .RuleFor(s => s.CustomerId, f => f.Random.Guid())
        .RuleFor(s => s.CustomerName, f => f.Person.FullName)
        .RuleFor(s => s.BranchId, f => f.Random.Guid())
        .RuleFor(s => s.BranchName, f => f.Company.CompanyName())
        .RuleFor(s => s.Items, f => SaleItemCommandFaker.Generate(f.Random.Int(1, 3)));

    public static CreateSaleCommand GenerateValidCommand(int itemCount = 1)
    {
        var command = CreateSaleCommandFaker.Generate();
        command.Items = SaleItemCommandFaker.Generate(itemCount);
        return command;
    }

    public static SaleItemCommand GenerateValidItemCommand()
    {
        return SaleItemCommandFaker.Generate();
    }

    public static Sale CreateValidSale(int itemCount = 1)
    {
        var faker = new Faker();
        var items = Enumerable.Range(0, itemCount)
            .Select(_ => new SaleItem(
                faker.Random.Guid(),
                faker.Commerce.ProductName(),
                faker.Random.Int(1, 9),
                faker.Finance.Amount(1m, 100m))
            { Id = Guid.NewGuid() })
            .ToList();

        return new Sale(
            $"SALE-{faker.Random.AlphaNumeric(8).ToUpper()}",
            DateTime.UtcNow,
            faker.Random.Guid(),
            faker.Person.FullName,
            faker.Random.Guid(),
            faker.Company.CompanyName(),
            items);
    }

    public static Sale CreateCancelledSale()
    {
        var sale = CreateValidSale();
        sale.Cancel();
        return sale;
    }
}
