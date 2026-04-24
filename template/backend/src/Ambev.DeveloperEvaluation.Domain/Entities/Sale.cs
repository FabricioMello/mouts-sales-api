using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class Sale : BaseEntity
{
    private readonly List<SaleItem> _items = [];

    public string SaleNumber { get; private set; } = string.Empty;
    public DateTime SaleDate { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public Guid BranchId { get; private set; }
    public string BranchName { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    private Sale()
    {
    }

    public Sale(
        string saleNumber,
        DateTime saleDate,
        Guid customerId,
        string customerName,
        Guid branchId,
        string branchName,
        IEnumerable<SaleItem> items)
    {
        CreatedAt = DateTime.UtcNow;
        UpdateDetails(saleNumber, saleDate, customerId, customerName, branchId, branchName, items, false);
    }

    public void UpdateDetails(
        string saleNumber,
        DateTime saleDate,
        Guid customerId,
        string customerName,
        Guid branchId,
        string branchName,
        IEnumerable<SaleItem> items,
        bool touchUpdatedAt = true)
    {
        SaleNumber = saleNumber;
        SaleDate = NormalizeDate(saleDate);
        CustomerId = customerId;
        CustomerName = customerName;
        BranchId = branchId;
        BranchName = branchName;

        _items.Clear();
        _items.AddRange(items);

        Validate();
        RecalculateTotal();

        if (touchUpdatedAt)
            UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        IsCancelled = true;

        foreach (var item in _items)
            item.Cancel();

        UpdatedAt = DateTime.UtcNow;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(SaleNumber))
            throw new InvalidOperationException("Sale number is required");

        if (CustomerId == Guid.Empty)
            throw new InvalidOperationException("Customer ID is required");

        if (string.IsNullOrWhiteSpace(CustomerName))
            throw new InvalidOperationException("Customer name is required");

        if (BranchId == Guid.Empty)
            throw new InvalidOperationException("Branch ID is required");

        if (string.IsNullOrWhiteSpace(BranchName))
            throw new InvalidOperationException("Branch name is required");

        if (_items.Count == 0)
            throw new InvalidOperationException("Sale must have at least one item");
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(item => item.TotalAmount);
    }

    private static DateTime NormalizeDate(DateTime date)
    {
        return date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : date.ToUniversalTime();
    }
}
