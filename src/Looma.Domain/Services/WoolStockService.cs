// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Logging;
using Looma.Domain.Refresh;
using Looma.Domain.Repositories;
using Looma.Domain.Request;

namespace Looma.Domain.Services;

public class WoolStockService(
    IWoolUsageRepository repository,
    IDomainLogger? logger = null,
    IDataRefreshService? refreshService = null)
    : DomainServiceBase(logger), IWoolStockService
{
    public async Task<Result> AdjustWoolUsageAsync(AdjustProjectWoolUsageRequest request)
    {
        var result = await ExecuteAsync($"WoolUsage.Adjust(project:{request.ProjectId}, wool:{request.WoolId})", async () =>
        {
            if (request.Quantity <= 0)
            {
                return Result.Failure("La quantité doit être supérieure à zéro.");
            }

            var usageResult = await repository.GetUsageAsync(request.ProjectId, request.WoolId);
            if (usageResult.Failed || usageResult.Value is null)
            {
                return Result.Failure("Une erreur est survenue lors de la récupération de l'usage de la laine.");
            }

            var usage = usageResult.Value;
            var factor = request.Mode switch
            {
                StockAdjustmentMode.ByBall => 0,
                StockAdjustmentMode.ByWeight => usage.Wool.Weight,
                StockAdjustmentMode.ByLength => usage.Wool.Length,
                _ => 0
            };
            var delta = ComputeStockQuantity(request.Mode, request.IsAddition, request.Quantity, factor);

            if (!request.IsAddition && Math.Abs(delta) > usage.StockUsed)
            {
                delta = -usage.StockUsed;
            }

            if (request is { IsAddition: true, DeductImmediately: true } && delta > usage.Wool.Stock)
            {
                return Result.Failure("Le stock disponible est insuffisant.");
            }

            if (request is { IsAddition: false, DeductImmediately: true })
            {
                var restore = Math.Min(Math.Abs(delta), usage.StockAlreadyUsed);
                var result = await repository.UpdateCurrentStockUsageAsync(request.ProjectId, request.WoolId, -restore);
                if (result.Failed)
                {
                    return Result.Failure(result.Error ?? "Erreur inconnue");
                }
            }

            var updateResult = await repository.UpdateStockUsedAsync(request.ProjectId, request.WoolId, usage.StockUsed + delta);
            if (updateResult.Failed)
            {
                return Result.Failure(updateResult.Error ?? "Erreur inconnue");
            }

            if (request.IsAddition && request.DeductImmediately)
            {
                var result = await repository.UpdateCurrentStockUsageAsync(request.ProjectId, request.WoolId, delta);
                if (result.Failed)
                {
                    return Result.Failure(result.Error ?? "Erreur inconnue");
                }
            }

            if (usage.StockAlreadyUsed > usage.StockUsed)
            {
                var result = await repository.UpdateStockAlreadyUsedAsync(request.ProjectId, request.WoolId, usage.StockUsed);
                if (result.Failed)
                {
                    return Result.Failure(result.Error ?? "Erreur inconnue");
                }
            }

            return Result.Ok();
        });

        var scope = RefreshScope.Projects;
        if (request.DeductImmediately)
            scope |= RefreshScope.Wools;

        if (result.Succeeded)
            refreshService?.RequestRefresh(scope, $"Wool usage changed for project {request.ProjectId}.");

        return result;
    }

    private static double ComputeStockQuantity(StockAdjustmentMode mode, bool isAddition, double quantity, double factor)
    {
        var data = mode switch
        {
            StockAdjustmentMode.ByBall => quantity * 1000,
            StockAdjustmentMode.ByWeight or StockAdjustmentMode.ByLength => quantity / factor * 1000,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return isAddition ? data : -data;
    }
}
