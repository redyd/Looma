// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Repositories;
using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Repositories;

public sealed class TrackedWoolRepository(LoomaDbContext context) : ITrackedWoolRepository
{
    public async Task<Result> AddAsync(int woolId, double quantity, int? projectId = null)
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
                Date = DateTime.UtcNow,
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
