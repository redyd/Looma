// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Request;
using Looma.Infrastructure.Entity;
using Looma.Infrastructure.Mapping;
using Looma.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Repositories;

public class ProjectRepository(LoomaDbContext context, AppPaths pathManager) : IProjectRepository
{
    public async Task<ResultT<IReadOnlyList<Project>>> GetAllAsync()
    {
        try
        {
            var entities = await IncludeDetails(context.Projects)
                .OrderBy(p => p.BeginDate == null)
                .ThenBy(p => p.BeginDate)
                .ThenBy(p => p.Name)
                .ToListAsync();

            if (DocumentMetadataBackfill.Apply(entities.SelectMany(p => p.Files), pathManager))
                await context.SaveChangesAsync();

            return ResultT<IReadOnlyList<Project>>.Ok(entities.Select(p => ApplyFileMetadata(p.ToDomain())).ToList());
        }
        catch (Exception ex)
        {
            return ResultT<IReadOnlyList<Project>>.Failure($"Impossible de charger les projets: {ex.Message}");
        }
    }

    public async Task<ResultT<Project>> GetByIdAsync(int id)
    {
        try
        {
            var entity = await IncludeDetails(context.Projects)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (entity is not null && DocumentMetadataBackfill.Apply(entity.Files, pathManager))
                await context.SaveChangesAsync();

            return entity is null
                ? ResultT<Project>.NotFound($"Le projet {id} est introuvable.")
                : ResultT<Project>.Ok(ApplyFileMetadata(entity.ToDomain()));
        }
        catch (Exception ex)
        {
            return ResultT<Project>.Failure($"Impossible de charger le projet {id}: {ex.Message}");
        }
    }

    public async Task<ResultT<Project>> AddAsync(CreateProjectRequest request)
    {
        try
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ResultT<Project>.Failure("Le nom du projet est invalide.");

            if (!await context.Patterns.AnyAsync(p => p.PatternId == request.PatternId))
                return ResultT<Project>.NotFound($"Le patron {request.PatternId} est introuvable.");

            var woolIds = request.WoolIds.Distinct().ToList();
            if (!await AllWoolsExistAsync(woolIds))
                return ResultT<Project>.Failure("Une ou plusieurs laines sélectionnées sont introuvables.");

            var entity = new ProjectEntity
            {
                Name = name,
                Status = request.Status,
                Note = NormalizeOptional(request.Note),
                BeginDate = request.BeginDate,
                EndDate = request.EndDate,
                PatternId = request.PatternId,
                WoolsForProjects = woolIds
                    .Select(woolId => new WoolsForProjectEntity { WoolId = woolId, StockUsed = 0 })
                    .ToList()
            };

            context.Projects.Add(entity);
            await context.SaveChangesAsync();
            return await GetByIdAsync(entity.ProjectId);
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Project>.Failure($"Impossible d'ajouter le projet: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Project>.Failure($"Impossible d'ajouter le projet: {ex.Message}");
        }
    }

    public async Task<ResultT<Project>> UpdateAsync(UpdateProjectRequest request)
    {
        try
        {
            var entity = await context.Projects
                .Include(p => p.WoolsForProjects)
                .FirstOrDefaultAsync(p => p.ProjectId == request.Id);
            if (entity is null)
                return ResultT<Project>.NotFound($"Le projet {request.Id} est introuvable.");

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ResultT<Project>.Failure("Le nom du projet est invalide.");

            if (!await context.Patterns.AnyAsync(p => p.PatternId == request.PatternId))
                return ResultT<Project>.NotFound($"Le patron {request.PatternId} est introuvable.");

            var woolIds = request.WoolIds.Distinct().ToList();
            if (!await AllWoolsExistAsync(woolIds))
                return ResultT<Project>.Failure("Une ou plusieurs laines sélectionnées sont introuvables.");

            entity.Name = name;
            entity.Status = request.Status;
            entity.Note = NormalizeOptional(request.Note);
            entity.BeginDate = request.BeginDate;
            entity.EndDate = request.EndDate;
            entity.PatternId = request.PatternId;
            SyncWools(entity, woolIds);

            await context.SaveChangesAsync();
            return await GetByIdAsync(entity.ProjectId);
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Project>.Failure($"Impossible de mettre à jour le projet {request.Id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Project>.Failure($"Impossible de mettre à jour le projet {request.Id}: {ex.Message}");
        }
    }

    public async Task<ResultT<Project>> UpdateWoolUsageAsync(int projectId, int woolId, double stockUsed)
    {
        try
        {
            var usage = await context.WoolsForProjects
                .FirstOrDefaultAsync(w => w.ProjectId == projectId && w.WoolId == woolId);
            if (usage is null)
                return ResultT<Project>.NotFound("La laine sélectionnée n'est pas liée à ce projet.");

            usage.StockUsed = Math.Max(0, stockUsed);
            await context.SaveChangesAsync();
            return await GetByIdAsync(projectId);
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Project>.Failure($"Impossible de mettre à jour l'utilisation de laine: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Project>.Failure($"Impossible de mettre à jour l'utilisation de laine: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            var entity = await context.Projects.FindAsync(id);
            if (entity is null)
                return Result.NotFound($"Le projet {id} est introuvable.");

            context.Projects.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible de supprimer le projet {id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible de supprimer le projet {id}: {ex.Message}");
        }
    }

    private static IQueryable<ProjectEntity> IncludeDetails(IQueryable<ProjectEntity> query) =>
        query
            .Include(p => p.PatternEntity)
            .ThenInclude(p => p.Documents)
            .Include(p => p.Files)
            .Include(p => p.WoolsForProjects)
            .ThenInclude(w => w.WoolEntity);

    private async Task<bool> AllWoolsExistAsync(IReadOnlyCollection<int> woolIds)
    {
        if (woolIds.Count == 0)
            return true;

        var existingCount = await context.Wools.CountAsync(w => woolIds.Contains(w.WoolId));
        return existingCount == woolIds.Count;
    }

    private static void SyncWools(ProjectEntity entity, IReadOnlyCollection<int> woolIds)
    {
        var selected = woolIds.ToHashSet();
        var removed = entity.WoolsForProjects
            .Where(w => !selected.Contains(w.WoolId))
            .ToList();

        foreach (var usage in removed)
            entity.WoolsForProjects.Remove(usage);

        var existing = entity.WoolsForProjects.Select(w => w.WoolId).ToHashSet();
        foreach (var woolId in selected.Where(woolId => !existing.Contains(woolId)))
            entity.WoolsForProjects.Add(new WoolsForProjectEntity { WoolId = woolId, StockUsed = 0 });
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Project ApplyFileMetadata(Project project) =>
        new()
        {
            ProjectId = project.ProjectId,
            Name = project.Name,
            Status = project.Status,
            Note = project.Note,
            BeginDate = project.BeginDate,
            EndDate = project.EndDate,
            Pattern = project.Pattern,
            Wools = project.Wools,
            Files = project.Files.Select(ApplyFileMetadata).ToList()
        };

    private Document ApplyFileMetadata(Document document)
    {
        var filePath = pathManager.GetDocumentStoragePath(document.Id);
        if (!File.Exists(filePath))
        {
            return new Document
            {
                Id = document.Id,
                Nickname = document.Nickname,
                Type = "Inconnu",
                SizeBytes = 0,
                StoragePath = null,
                PatternId = document.PatternId,
                PatternName = document.PatternName,
                ProjectId = document.ProjectId,
                ProjectName = document.ProjectName
            };
        }

        var info = new FileInfo(filePath);
        return new Document
        {
            Id = document.Id,
            Nickname = document.Nickname,
            Type = DocumentMetadataBackfill.GetDocumentType(filePath),
            SizeBytes = info.Length,
            StoragePath = filePath,
            PatternId = document.PatternId,
            PatternName = document.PatternName,
            ProjectId = document.ProjectId,
            ProjectName = document.ProjectName
        };
    }
}
