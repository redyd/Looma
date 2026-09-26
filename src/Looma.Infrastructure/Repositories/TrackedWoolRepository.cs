// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Localization;
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
            var tracked = context.TrackedWools.AsNoTracking();
            if (from is not null)
                tracked = tracked.Where(t => t.Date >= from.Value);

            // Jointure gauche : les mouvements d'une laine supprimée (WoolId null) restent visibles grâce aux valeurs figées.
            var rows = await (
                    from t in tracked
                    orderby t.Date
                    select new
                    {
                        Movement = t,
                        Wool = t.WoolEntity,
                        LiveProjectName = t.ProjectEntity == null ? null : t.ProjectEntity.Name,
                        LiveProjectStatus = t.ProjectEntity == null ? (Status?)null : t.ProjectEntity.Status,
                        LivePatternType = t.ProjectEntity == null || t.ProjectEntity.PatternEntity == null
                            ? (PatternType?)null
                            : t.ProjectEntity.PatternEntity.Type
                    })
                .ToListAsync();

            var movements = rows
                .Select(row =>
                {
                    var t = row.Movement;
                    var w = row.Wool;
                    return new TrackedWoolMovement(
                        t.Id,
                        t.Date,
                        t.Quantity,
                        t.WoolId,
                        t.WoolName ?? w?.Name ?? string.Empty,
                        t.WoolBrand ?? w?.Brand ?? string.Empty,
                        t.ProjectId,
                        row.LiveProjectName ?? t.ProjectName,
                        row.LiveProjectStatus,
                        row.LivePatternType ?? t.PatternType)
                    {
                        WoolMaterial = t.WoolMaterial ?? w?.Material ?? string.Empty,
                        WoolColors = SplitColors(t.WoolColor ?? w?.Color),
                        WoolWeight = t.WoolWeight ?? w?.Weight ?? 0,
                        WoolLength = t.WoolLength ?? w?.Length ?? 0,
                        WoolNeedleMinSize = t.WoolNeedleMinSize ?? w?.NeedleMinSize ?? 0,
                        WoolNeedleMaxSize = t.WoolNeedleMaxSize ?? w?.NeedleMaxSize ?? 0
                    };
                })
                .ToList();

            return ResultT<IReadOnlyList<TrackedWoolMovement>>.Ok(movements);
        }
        catch (Exception ex)
        {
            return ResultT<IReadOnlyList<TrackedWoolMovement>>.Failure(
                Localizer.Format("Statistics_Errors_UnableToLoadMovements", ex.Message));
        }
    }

    public async Task<Result> AddAsync(int woolId, double quantity, int? projectId = null, DateTime? date = null)
    {
        try
        {
            var wool = await context.Wools.AsNoTracking().FirstOrDefaultAsync(w => w.WoolId == woolId);
            if (wool is null)
                return Result.NotFound($"La laine {woolId} est introuvable.");

            string? projectName = null;
            PatternType? patternType = null;
            if (projectId is not null)
            {
                var project = await context.Projects
                    .AsNoTracking()
                    .Where(p => p.ProjectId == projectId.Value)
                    .Select(p => new
                    {
                        p.Name,
                        PatternType = p.PatternEntity == null ? (PatternType?)null : p.PatternEntity.Type
                    })
                    .FirstOrDefaultAsync();

                if (project is null)
                    return Result.NotFound($"Le projet {projectId.Value} est introuvable.");

                projectName = project.Name;
                patternType = project.PatternType;
            }

            context.TrackedWools.Add(new TrackedWool
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = date ?? DateTime.UtcNow,
                Quantity = quantity,
                WoolId = woolId,
                ProjectId = projectId,
                WoolName = wool.Name,
                WoolBrand = wool.Brand,
                WoolMaterial = wool.Material,
                WoolColor = wool.Color,
                WoolWeight = wool.Weight,
                WoolLength = wool.Length,
                WoolNeedleMinSize = wool.NeedleMinSize,
                WoolNeedleMaxSize = wool.NeedleMaxSize,
                ProjectName = projectName,
                PatternType = patternType
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

    private static IReadOnlyList<string> SplitColors(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split('|').Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
}
