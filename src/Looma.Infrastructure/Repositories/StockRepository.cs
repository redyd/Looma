using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly LoomaDbContext _context;

    public StockRepository(LoomaDbContext context) => _context = context;

    public async Task<IReadOnlyList<Stock>> GetByWoolIdAsync(int woolId, CancellationToken ct = default)
    {
        var entities = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.WoolId == woolId)
            .ToListAsync(ct);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<double> GetTotalWeightByWoolIdAsync(int woolId, CancellationToken ct = default) =>
        await _context.Stocks
            .Where(s => s.WoolId == woolId)
            .SumAsync(s => s.WeightQuantity, ct);

    public async Task AddAsync(Stock stock, CancellationToken ct = default)
    {
        _context.Stocks.Add(stock.ToEntity());
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Stock stock, CancellationToken ct = default)
    {
        var entity = await _context.Stocks.FindAsync([stock.Id], ct)
                     ?? throw new InvalidOperationException($"Stock {stock.Id} introuvable.");
        entity.UpdateEntity(stock);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int stockId, CancellationToken ct = default)
    {
        var entity = await _context.Stocks.FindAsync([stockId], ct)
                     ?? throw new InvalidOperationException($"Stock {stockId} introuvable.");
        _context.Stocks.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }
}