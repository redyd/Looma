using Looma.Domain.Entities;

namespace Looma.Domain.Repositories;

public interface IStockRepository
{
    Task<IReadOnlyList<Stock>> GetByWoolIdAsync(int woolId, CancellationToken ct = default);
    Task<double> GetTotalWeightByWoolIdAsync(int woolId, CancellationToken ct = default);
    Task AddAsync(Stock stock, CancellationToken ct = default);
    Task UpdateAsync(Stock stock, CancellationToken ct = default);
    Task DeleteAsync(int stockId, CancellationToken ct = default);
}