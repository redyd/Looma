using Looma.Domain.Entities;

namespace Looma.Domain.Repositories;

public interface IWoolRepository
{
    Task<IReadOnlyList<Wool>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Wool>> SearchAsync(string query, CancellationToken ct = default);
    Task<Wool?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Wool wool, CancellationToken ct = default);
    Task UpdateAsync(Wool wool, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}