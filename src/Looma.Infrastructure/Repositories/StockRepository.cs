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
            return ResultT<IReadOnlyList<Stock>>.Failure(
                $"Impossible de charger les stocks de la laine {woolId}: {ex.Message}");
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

    public async Task<ResultT<Stock>> AddAsync(CreateStockRequest request)
    {
        try
        {
            if (!IsValid(request.WoolId, request.WeightGrams))
                return ResultT<Stock>.Failure("Les donnees de creation de stock sont invalides.");

            var woolExists = await context.Wools.AnyAsync(w => w.WoolId == request.WoolId);
            if (!woolExists)
                return ResultT<Stock>.NotFound($"La laine {request.WoolId} est introuvable.");

            var stock = new Stock
            {
                Id = 0,
                WoolId = request.WoolId,
                WeightGrams = request.WeightGrams
            };

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

    public async Task<ResultT<Stock>> UpdateAsync(UpdateStockRequest request)
    {
        try
        {
            var entity = await context.Stocks.FindAsync(request.Id);
            if (entity is null)
                return ResultT<Stock>.NotFound($"Le stock {request.Id} est introuvable.");

            var woolExists = await context.Wools.AnyAsync(w => w.WoolId == entity.WoolId);
            if (!woolExists)
                return ResultT<Stock>.NotFound($"La laine {entity.WoolId} est introuvable.");

            entity.WeightQuantity = request.WeightGrams;
            
            await context.SaveChangesAsync();
            return ResultT<Stock>.Ok(entity.ToDomain());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return ResultT<Stock>.Failure($"Impossible de mettre à jour le stock {request.Id}: {ex.Message}");
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Stock>.Failure($"Impossible de mettre à jour le stock {request.Id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Stock>.Failure($"Impossible de mettre à jour le stock {request.Id}: {ex.Message}");
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

    private static bool IsValid(int woolId, double weightGrams)
    {
        return woolId > 0 && weightGrams > 0;
    }
}