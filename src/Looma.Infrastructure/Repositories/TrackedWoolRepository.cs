// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Repositories;

public sealed class TrackedWoolRepository(LoomaDbContext context) : ITrackedWoolRepository
{
    public async Task<ResultT<IReadOnlyList<TrackedWoolMovement>>> GetMovementsAsync(DateTime? from = null)
    {
        try
        {
            var query = context.TrackedWools
                .AsNoTracking()
                .Include(t => t.WoolEntity)
                .Include(t => t.ProjectEntity)
                .ThenInclude(p => p!.PatternEntity)
                .AsQueryable();

            if (from is not null)
                query = query.Where(t => t.Date >= from.Value);

            var movements = await query
                .OrderBy(t => t.Date)
                .ThenBy(t => t.WoolEntity.Brand)
                .ThenBy(t => t.WoolEntity.Name)
                .Select(t => new TrackedWoolMovement(
                    t.Id,
                    t.Date,
                    t.Quantity,
                    t.WoolId,
                    t.WoolEntity.Name,
                    t.WoolEntity.Brand,
                    t.ProjectId,
                    t.ProjectEntity == null ? null : t.ProjectEntity.Name,
                    t.ProjectEntity == null ? null : t.ProjectEntity.Status,
                    t.ProjectEntity == null || t.ProjectEntity.PatternEntity == null
                        ? null
                        : t.ProjectEntity.PatternEntity.Type))
                .ToListAsync();

            return ResultT<IReadOnlyList<TrackedWoolMovement>>.Ok(movements);
        }
        catch (Exception ex)
        {
            return ResultT<IReadOnlyList<TrackedWoolMovement>>.Failure(
                $"Impossible de charger les mouvements de laine: {ex.Message}");
        }
    }

    public async Task<Result> AddAsync(int woolId, double quantity, int? projectId = null, DateTime? date = null)
    {
        try
        {
            if (!await context.Wools.AnyAsync(w => w.WoolId == woolId))
                return Result.NotFound($"La laine {woolId} est introuvable.");

            if (projectId is not null && !await context.Projects.AnyAsync(p => p.ProjectId == projectId.Value))
                return Result.NotFound($"Le projet {projectId.Value} est introuvable.");

            context.TrackedWools.Add(new TrackedWool
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = date ?? DateTime.UtcNow,
                Quantity = quantity,
                WoolId = woolId,
                ProjectId = projectId
            });

            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible d'ajouter le suivi de stock de laine: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible d'ajouter le suivi de stock de laine: {ex.Message}");
        }
    }
}
