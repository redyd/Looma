using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Repositories;

public class WoolRepository : IWoolRepository
{
    private readonly LoomaDbContext _context;

    public WoolRepository(LoomaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Wool>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _context.Wools
            .AsNoTracking()
            .OrderBy(w => w.Brand)
            .ThenBy(w => w.Name)
            .ToListAsync(ct);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Wool>> SearchAsync(string query, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return WoolSearchSpec.Apply(all, query).ToList();
    }

    public async Task<Wool?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.Wools
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WoolId == id, ct);

        return entity?.ToDomain();
    }

    public async Task AddAsync(Wool wool, CancellationToken ct = default)
    {
        var entity = wool.ToEntity();
        _context.Wools.Add(entity);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Wool wool, CancellationToken ct = default)
    {
        var entity = await _context.Wools
            .FirstOrDefaultAsync(w => w.WoolId == wool.Id, ct);

        if (entity is null)
            throw new InvalidOperationException($"Laine {wool.Id} introuvable.");

        entity.UpdateEntity(wool);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.Wools.FindAsync([id], ct);

        if (entity is null)
            throw new InvalidOperationException($"Laine {id} introuvable.");

        _context.Wools.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }
}