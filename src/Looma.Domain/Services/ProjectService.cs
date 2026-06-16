// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;
using Looma.Domain.Request;

namespace Looma.Domain.Services;

public class ProjectService(
    IProjectRepository repo,
    IWoolRepository woolRepository,
    IWoolUsageRepository woolUsageRepository,
    IDomainLogger? logger = null)
    : DomainServiceBase(logger), IProjectService
{
    public Task<ResultT<IReadOnlyList<Project>>> GetAllAsync() =>
        ExecuteAsync("Projects.GetAll", repo.GetAllAsync);

    public Task<ResultT<Project>> GetByIdAsync(int id) =>
        ExecuteAsync($"Projects.GetById({id})", () => repo.GetByIdAsync(id));

    public Task<ResultT<Project>> AddAsync(CreateProjectRequest request) =>
        ExecuteAsync("Projects.Add", () => repo.AddAsync(request));

    public Task<Result> DeleteAsync(int id) =>
        ExecuteAsync($"Projects.Delete({id})", () => repo.DeleteAsync(id));

    public Task<ResultT<Project>> UpdateAsync(UpdateProjectRequest request) =>
        ExecuteAsync($"Projects.Update({request.Id})", async () =>
        {
            var existing = await repo.GetByIdAsync(request.Id);
            if (existing.Failed || existing.Value is null)
            {
                return ResultT<Project>.NotFound("Projet non trouvé.");
            }

            if (request.Status == Status.Finished && existing.Value.Status != Status.Finished)
            {
                var completeResult = await CompleteProjectAsync(request.Id, [.. existing.Value.Wools.Select(w => w.Wool)]);
                if (completeResult.Failed)
                {
                    return ResultT<Project>.Failure(completeResult.Error ?? "Impossible de terminer le projet.");
                }
            }

            return await repo.UpdateAsync(request);
        });

    private async Task<Result> CompleteProjectAsync(int projectId, IEnumerable<Wool> wools)
    {
        Logger.Log(DomainLogLevel.Information, $"Projects.Complete({projectId}) started.");

        foreach (var wool in wools)
        {
            var usageResult = await woolUsageRepository.GetUsageAsync(projectId, wool.Id);
            if (usageResult.Failed)
            {
                Logger.Log(DomainLogLevel.Warning, $"Projects.Complete({projectId}) failed while reading wool usage {wool.Id}.");
                return Result.Failure(usageResult.Error ?? "Erreur lors de la récupération de l'usage.");
            }

            var usage = usageResult.Value!;
            var remainingToDeduct = Math.Max(0, usage.StockUsed - usage.StockAlreadyUsed);
            if (remainingToDeduct <= 0)
                continue;

            if (wool.Stock < remainingToDeduct)
            {
                Logger.Log(DomainLogLevel.Warning, $"Projects.Complete({projectId}) failed because wool {wool.Id} stock is insufficient.");
                return Result.Failure($"Le stock disponible est insuffisant pour {wool.Name}.");
            }

            var stockResult = await woolRepository.AddStock(wool.Id, -remainingToDeduct);
            if (stockResult.Failed)
            {
                Logger.Log(DomainLogLevel.Warning, $"Projects.Complete({projectId}) failed while updating stock for wool {wool.Id}.");
                return Result.Failure(stockResult.Error ?? "Erreur lors de la mise à jour du stock.");
            }

            var alreadyUsedResult = await woolUsageRepository.UpdateStockAlreadyUsedAsync(projectId, wool.Id, usage.StockAlreadyUsed + remainingToDeduct);
            if (alreadyUsedResult.Failed)
            {
                Logger.Log(DomainLogLevel.Warning, $"Projects.Complete({projectId}) failed while updating already used stock for wool {wool.Id}.");
                return Result.Failure(alreadyUsedResult.Error ?? "Erreur lors de la mise à jour du stock déjà utilisé.");
            }
        }

        Logger.Log(DomainLogLevel.Information, $"Projects.Complete({projectId}) completed.");
        return Result.Ok();
    }
}
