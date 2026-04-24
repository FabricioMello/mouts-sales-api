using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercentage { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }

    public Sale Sale { get; set; } = null!;

    private SaleItem()
    {
    }

    public SaleItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;

        Recalculate();
    }

    public void Update(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;

        Recalculate();
    }

    public void Cancel()
    {
        IsCancelled = true;
    }

    private void Recalculate()
    {
        if (Quantity <= 0)
            throw new InvalidOperationException("Sale item quantity must be greater than zero");

        if (Quantity > 20)
            throw new InvalidOperationException("It is not possible to sell more than 20 identical items");

        if (UnitPrice <= 0)
            throw new InvalidOperationException("Sale item unit price must be greater than zero");

        DiscountPercentage = Quantity switch
        {
            >= 10 => 20m,
            >= 4 => 10m,
            _ => 0m
        };

        var grossAmount = Quantity * UnitPrice;
        DiscountAmount = grossAmount * (DiscountPercentage / 100m);
        TotalAmount = grossAmount - DiscountAmount;
    }
}
