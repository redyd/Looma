using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Repositories;

public class WoolUsageRepository(LoomaDbContext context) : IWoolUsageRepository
{

    public Task<ResultT<IReadOnlyList<WoolUsage>>> GetAllUsagesAsync(int projectId)
    {
        try
        {
            var result = context.WoolsForProjects
                .Where(w => w.ProjectId == projectId)
                .Include(w => w.WoolEntity)
                .Select(w => new WoolUsage
                {
                    Wool = w.WoolEntity.ToDomain(),
                    StockUsed = w.StockUsed,
                    StockAlreadyUsed = w.StockAlreadyUsed
                })
                .ToList();

            return Task.FromResult(ResultT<IReadOnlyList<WoolUsage>>.Ok(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ResultT<IReadOnlyList<WoolUsage>>.Failure(ex.Message));
        }
    }

    public Task<ResultT<WoolUsage>> GetUsageAsync(int projectId, int woolId)
    {
        try
        {
            var result = context.WoolsForProjects
                .Where(w => w.ProjectId == projectId && w.WoolId == woolId)
                .Include(w => w.WoolEntity)
                .Select(w => new WoolUsage
                {
                    Wool = w.WoolEntity.ToDomain(),
                    StockUsed = w.StockUsed,
                    StockAlreadyUsed = w.StockAlreadyUsed
                })
                .FirstOrDefault();

            if (result is null)
            {
                return Task.FromResult(ResultT<WoolUsage>.NotFound($"No usage found for project {projectId} and wool {woolId}"));
            }

            return Task.FromResult(ResultT<WoolUsage>.Ok(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ResultT<WoolUsage>.Failure(ex.Message));
        }
    }

    public async Task<Result> UpdateStockUsedAsync(int projectId, int woolId, double stockUsed)
    {
        try
        {
            var usage = await context.WoolsForProjects
                .FirstOrDefaultAsync(w => w.ProjectId == projectId && w.WoolId == woolId);
            if (usage is null)
            {
                return Result.NotFound("La laine sélectionnée n'est pas liée à ce projet.");
            }

            usage.StockUsed = Math.Max(0, stockUsed);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible de mettre à jour l'utilisation de laine: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible de mettre à jour l'utilisation de laine: {ex.Message}");
        }
    }

    public async Task<Result> UpdateCurrentStockUsageAsync(int projectId, int woolId, double stockUsed)
    {
        var usage = context.WoolsForProjects
            .Where(w => w.ProjectId == projectId && w.WoolId == woolId)
            .Include(w => w.WoolEntity)
            .FirstOrDefault();

        if (usage is null)
        {
            return Result.NotFound($"No usage found for project {projectId} and wool {woolId}");
        }

        usage.WoolEntity.Stock = Math.Max(0, usage.WoolEntity.Stock + stockUsed);
        usage.StockAlreadyUsed = Math.Max(0, usage.StockAlreadyUsed - stockUsed);

        await context.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> UpdateStockAlreadyUsedAsync(int projectId, int woolId, double stockAlreadyUsed)
    {
        var usage = context.WoolsForProjects
            .Where(w => w.ProjectId == projectId && w.WoolId == woolId)
            .Include(w => w.WoolEntity)
            .FirstOrDefault();

        if (usage is null)
        {
            return Result.NotFound($"No usage found for project {projectId} and wool {woolId}");
        }

        usage.StockAlreadyUsed = stockAlreadyUsed;

        await context.SaveChangesAsync();
        return Result.Ok();
    }
}
