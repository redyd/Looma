using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Repositories;

public class StockRepository(LoomaDbContext context) : IStockRepository
{
    public async Task<ResultT<IReadOnlyList<Stock>>> GetByWoolIdAsync(int woolId)
    {
        try
        {
            var entities = await context.Stocks
                .AsNoTracking()
                .Where(s => s.WoolId == woolId)
                .ToListAsync();

            return ResultT<IReadOnlyList<Stock>>.Ok(entities.Select(e => e.ToDomain()).ToList());
        }
        catch (Exception ex)
        {
            return ResultT<IReadOnlyList<Stock>>.Failure($"Impossible de charger les stocks de la laine {woolId}: {ex.Message}");
        }
    }

    public async Task<ResultT<double>> GetTotalWeightByWoolIdAsync(int woolId)
    {
        try
        {
            var total = await context.Stocks
                .Where(s => s.WoolId == woolId)
                .SumAsync(s => s.WeightQuantity);

            return ResultT<double>.Ok(total);
        }
        catch (Exception ex)
        {
            return ResultT<double>.Failure($"Impossible de calculer le poids total de la laine {woolId}: {ex.Message}");
        }
    }

    public async Task<ResultT<Stock>> AddAsync(Stock stock)
    {
        try
        {
            var woolExists = await context.Wools.AnyAsync(w => w.WoolId == stock.WoolId);
            if (!woolExists)
                return ResultT<Stock>.NotFound($"La laine {stock.WoolId} est introuvable.");

            var entity = stock.ToEntity();
            context.Stocks.Add(entity);
            await context.SaveChangesAsync();
            return ResultT<Stock>.Ok(entity.ToDomain());
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Stock>.Failure($"Impossible d'ajouter le stock: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Stock>.Failure($"Impossible d'ajouter le stock: {ex.Message}");
        }
    }

    public async Task<ResultT<Stock>> UpdateAsync(Stock stock)
    {
        try
        {
            var entity = await context.Stocks.FindAsync([stock.Id]);
            if (entity is null)
                return ResultT<Stock>.NotFound($"Le stock {stock.Id} est introuvable.");

            var woolExists = await context.Wools.AnyAsync(w => w.WoolId == stock.WoolId);
            if (!woolExists)
                return ResultT<Stock>.NotFound($"La laine {stock.WoolId} est introuvable.");

            entity.UpdateEntity(stock);
            await context.SaveChangesAsync();
            return ResultT<Stock>.Ok(entity.ToDomain());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return ResultT<Stock>.Failure($"Impossible de mettre à jour le stock {stock.Id}: {ex.Message}");
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Stock>.Failure($"Impossible de mettre à jour le stock {stock.Id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Stock>.Failure($"Impossible de mettre à jour le stock {stock.Id}: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int stockId)
    {
        try
        {
            var entity = await context.Stocks.FindAsync([stockId]);
            if (entity is null)
                return Result.NotFound($"Le stock {stockId} est introuvable.");

            context.Stocks.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible de supprimer le stock {stockId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible de supprimer le stock {stockId}: {ex.Message}");
        }
    }
}
