using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(sale => sale.Items)
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);
    }

    public async Task<Sale?> GetBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(sale => sale.Items)
            .FirstOrDefaultAsync(sale => sale.SaleNumber == saleNumber, cancellationToken);
    }

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        int page, int size, SaleFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Sales
            .Include(sale => sale.Items)
            .AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SaleNumber))
                query = query.Where(s => EF.Functions.ILike(s.SaleNumber, $"%{filter.SaleNumber}%"));

            if (filter.CustomerId.HasValue)
                query = query.Where(s => s.CustomerId == filter.CustomerId.Value);

            if (!string.IsNullOrWhiteSpace(filter.CustomerName))
                query = query.Where(s => EF.Functions.ILike(s.CustomerName, $"%{filter.CustomerName}%"));

            if (filter.BranchId.HasValue)
                query = query.Where(s => s.BranchId == filter.BranchId.Value);

            if (!string.IsNullOrWhiteSpace(filter.BranchName))
                query = query.Where(s => EF.Functions.ILike(s.BranchName, $"%{filter.BranchName}%"));

            if (filter.IsCancelled.HasValue)
                query = query.Where(s => s.IsCancelled == filter.IsCancelled.Value);

            if (filter.SaleDateFrom.HasValue)
                query = query.Where(s => s.SaleDate >= filter.SaleDateFrom.Value);

            if (filter.SaleDateTo.HasValue)
                query = query.Where(s => s.SaleDate <= filter.SaleDateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.SaleDate)
            .ThenBy(s => s.SaleNumber)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }
}
