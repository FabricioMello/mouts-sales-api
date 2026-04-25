using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;

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

    public void CancelItem(Guid itemId)
    {
        if (IsCancelled)
            throw new BusinessRuleViolationException("Cancelled sales cannot be modified");

        var item = _items.FirstOrDefault(item => item.Id == itemId);
        if (item is null)
            throw new EntityNotFoundException("SaleItem", itemId);

        if (item.IsCancelled)
            throw new BusinessRuleViolationException("Sale item is already cancelled");

        item.Cancel();
        RecalculateTotalFromActiveItems();
        UpdatedAt = DateTime.UtcNow;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(SaleNumber))
            throw new DomainException("Sale number is required");

        if (CustomerId == Guid.Empty)
            throw new DomainException("Customer ID is required");

        if (string.IsNullOrWhiteSpace(CustomerName))
            throw new DomainException("Customer name is required");

        if (BranchId == Guid.Empty)
            throw new DomainException("Branch ID is required");

        if (string.IsNullOrWhiteSpace(BranchName))
            throw new DomainException("Branch name is required");

        if (_items.Count == 0)
            throw new DomainException("Sale must have at least one item");

        if (_items.Select(item => item.ProductId).Distinct().Count() != _items.Count)
            throw new DomainException("Sale cannot contain duplicated products");
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(item => item.TotalAmount);
    }

    private void RecalculateTotalFromActiveItems()
    {
        TotalAmount = _items
            .Where(item => !item.IsCancelled)
            .Sum(item => item.TotalAmount);
    }

    private static DateTime NormalizeDate(DateTime date)
    {
        return date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : date.ToUniversalTime();
    }
}
