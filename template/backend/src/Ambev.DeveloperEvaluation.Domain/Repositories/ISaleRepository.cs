using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface ISaleRepository
{
    Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sale?> GetBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> ListAsync(int page, int size, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default);
}
