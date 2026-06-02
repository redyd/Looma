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

    public async Task<ResultT<Wool>> AddAsync(CreateWoolRequest request)
    {
        try
        {
            var wool = BuildCreate(request);
            if (wool is null)
                return ResultT<Wool>.Failure("Les donnees de creation de laine sont invalides.");

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

    public async Task<ResultT<Wool>> UpdateAsync(UpdateWoolRequest request)
    {
        try
        {
            var entity = await context.Wools.FirstOrDefaultAsync(w => w.WoolId == request.Id);
            if (entity is null)
                return ResultT<Wool>.NotFound($"La laine {request.Id} est introuvable.");

            var current = entity.ToDomain();
            var updated = BuildUpdate(current, request);
            if (updated is null)
                return ResultT<Wool>.Failure($"Les donnees de mise a jour de la laine {request.Id} sont invalides.");

            context.Entry(entity).CurrentValues.SetValues(updated.ToEntity());
            await context.SaveChangesAsync();
            return ResultT<Wool>.Ok(entity.ToDomain());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return ResultT<Wool>.Failure($"Impossible de mettre à jour la laine {request.Id}: {ex.Message}");
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Wool>.Failure($"Impossible de mettre à jour la laine {request.Id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Wool>.Failure($"Impossible de mettre à jour la laine {request.Id}: {ex.Message}");
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

    private static Wool? BuildCreate(CreateWoolRequest request)
    {
        if (!IsValid(request.Name, request.Brand, request.Material, request.Color,
                request.LengthToWeightRatio, request.NeedleMinSize, request.NeedleMaxSize))
        {
            return null;
        }

        return new Wool
        {
            Id = 0,
            Name = request.Name.Trim(),
            Brand = request.Brand.Trim(),
            Material = request.Material.Trim(),
            Color = request.Color.Trim(),
            LengthToWeightRatio = request.LengthToWeightRatio,
            NeedleMinSize = request.NeedleMinSize,
            NeedleMaxSize = request.NeedleMaxSize
        };
    }

    private static Wool? BuildUpdate(Wool current, UpdateWoolRequest request)
    {
        var name = request.Name ?? current.Name;
        var brand = request.Brand ?? current.Brand;
        var material = request.Material ?? current.Material;
        var color = request.Color ?? current.Color;
        var ratio = request.LengthToWeightRatio ?? current.LengthToWeightRatio;
        var needleMin = request.NeedleMinSize ?? current.NeedleMinSize;
        var needleMax = request.NeedleMaxSize ?? current.NeedleMaxSize;

        if (!IsValid(name, brand, material, color, ratio, needleMin, needleMax))
            return null;

        return new Wool
        {
            Id = current.Id,
            Name = name.Trim(),
            Brand = brand.Trim(),
            Material = material.Trim(),
            Color = color.Trim(),
            LengthToWeightRatio = ratio,
            NeedleMinSize = needleMin,
            NeedleMaxSize = needleMax
        };
    }

    private static bool IsValid(string name, string brand, string material, string color,
        double lengthToWeightRatio, double needleMinSize, double needleMaxSize)
    {
        return !string.IsNullOrWhiteSpace(name)
               && !string.IsNullOrWhiteSpace(brand)
               && !string.IsNullOrWhiteSpace(material)
               && !string.IsNullOrWhiteSpace(color)
               && lengthToWeightRatio > 0
               && needleMinSize > 0
               && needleMaxSize > 0
               && needleMinSize <= needleMaxSize;
    }
}
