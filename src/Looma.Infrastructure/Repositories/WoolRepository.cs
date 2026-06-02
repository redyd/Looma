using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Repositories;

public class WoolRepository(LoomaDbContext context) : IWoolRepository
{
    public async Task<ResultT<IReadOnlyList<Wool>>> GetAllAsync()
    {
        try
        {
            var entities = await context.Wools
                .AsNoTracking()
                .OrderBy(w => w.Brand)
                .ThenBy(w => w.Name)
                .ToListAsync();

            return ResultT<IReadOnlyList<Wool>>.Ok(entities.Select(e => e.ToDomain()).ToList());
        }
        catch (Exception ex)
        {
            return ResultT<IReadOnlyList<Wool>>.Failure($"Impossible de charger les laines: {ex.Message}");
        }
    }

    public async Task<ResultT<IReadOnlyList<Wool>>> SearchAsync(string query)
    {
        var all = await GetAllAsync();
        if (all.Failed)
            return ResultT<IReadOnlyList<Wool>>.Failure(all.Error ?? "La recherche des laines a échoué.");

        return ResultT<IReadOnlyList<Wool>>.Ok(WoolSearchSpec.Apply(all.Value ?? [], query).ToList());
    }

    public async Task<ResultT<Wool>> GetByIdAsync(int id)
    {
        try
        {
            var entity = await context.Wools
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WoolId == id);

            return entity is null
                ? ResultT<Wool>.NotFound($"La laine {id} est introuvable.")
                : ResultT<Wool>.Ok(entity.ToDomain());
        }
        catch (Exception ex)
        {
            return ResultT<Wool>.Failure($"Impossible de charger la laine {id}: {ex.Message}");
        }
    }

    public async Task<ResultT<Wool>> AddAsync(Wool wool)
    {
        try
        {
            var entity = wool.ToEntity();
            context.Wools.Add(entity);
            await context.SaveChangesAsync();
            return ResultT<Wool>.Ok(entity.ToDomain());
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Wool>.Failure($"Impossible d'ajouter la laine: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Wool>.Failure($"Impossible d'ajouter la laine: {ex.Message}");
        }
    }

    public async Task<ResultT<Wool>> UpdateAsync(Wool wool)
    {
        try
        {
            var entity = await context.Wools
                .FirstOrDefaultAsync(w => w.WoolId == wool.Id);

            if (entity is null)
                return ResultT<Wool>.NotFound($"La laine {wool.Id} est introuvable.");

            entity.UpdateEntity(wool);
            await context.SaveChangesAsync();
            return ResultT<Wool>.Ok(entity.ToDomain());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return ResultT<Wool>.Failure($"Impossible de mettre à jour la laine {wool.Id}: {ex.Message}");
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Wool>.Failure($"Impossible de mettre à jour la laine {wool.Id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Wool>.Failure($"Impossible de mettre à jour la laine {wool.Id}: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            var entity = await context.Wools.FindAsync([id]);

            if (entity is null)
                return Result.NotFound($"La laine {id} est introuvable.");

            context.Wools.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible de supprimer la laine {id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible de supprimer la laine {id}: {ex.Message}");
        }
    }
}
