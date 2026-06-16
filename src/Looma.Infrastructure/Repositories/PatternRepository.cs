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

public class PatternRepository(LoomaDbContext context, AppPaths pathManager) : IPatternRepository
{
    public async Task<ResultT<IReadOnlyList<Pattern>>> GetAllAsync()
    {
        try
        {
            var entities = await context.Patterns
                .Include(p => p.Documents)
                .Include(p => p.Projects)
                .OrderBy(p => p.Name)
                .ToListAsync();

            if (DocumentMetadataBackfill.Apply(entities.SelectMany(p => p.Documents), pathManager))
                await context.SaveChangesAsync();

            return ResultT<IReadOnlyList<Pattern>>.Ok(
                entities.Select(e => ApplyFileMetadata(e.ToDomain())).ToList());
        }
        catch (Exception ex)
        {
            return ResultT<IReadOnlyList<Pattern>>.Failure($"Impossible de charger les patrons: {ex.Message}");
        }
    }

    public async Task<ResultT<Pattern>> GetByIdAsync(int id)
    {
        try
        {
            var entity = await context.Patterns
                .Include(p => p.Documents)
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.PatternId == id);

            if (entity is not null && DocumentMetadataBackfill.Apply(entity.Documents, pathManager))
                await context.SaveChangesAsync();

            return entity is null
                ? ResultT<Pattern>.NotFound($"Le patron {id} est introuvable.")
                : ResultT<Pattern>.Ok(ApplyFileMetadata(entity.ToDomain()));
        }
        catch (Exception ex)
        {
            return ResultT<Pattern>.Failure($"Impossible de charger le patron {id}: {ex.Message}");
        }
    }

    public async Task<ResultT<Pattern>> AddAsync(CreatePatternRequest request)
    {
        try
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ResultT<Pattern>.Failure("Le nom du patron est invalide.");

            var entity = new PatternEntity
            {
                Name = name,
                Url = NormalizeOptional(request.Url),
                Note = NormalizeOptional(request.Note),
                Type = request.Type,
                IsPersonal = request.IsPersonal,
                BeginDate = request.BeginDate,
                EndDate = request.EndDate,
                Projects = []
            };

            context.Patterns.Add(entity);
            await context.SaveChangesAsync();

            var saved = await context.Patterns.AsNoTracking()
                .Include(p => p.Documents)
                .Include(p => p.Projects)
                .FirstAsync(p => p.PatternId == entity.PatternId);

            return ResultT<Pattern>.Ok(ApplyFileMetadata(saved.ToDomain()));
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Pattern>.Failure($"Impossible d'ajouter le patron: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Pattern>.Failure($"Impossible d'ajouter le patron: {ex.Message}");
        }
    }

    public async Task<ResultT<Pattern>> UpdateAsync(UpdatePatternRequest request)
    {
        try
        {
            var entity = await context.Patterns
                .Include(p => p.Documents)
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.PatternId == request.Id);
            if (entity is null)
                return ResultT<Pattern>.NotFound($"Le patron {request.Id} est introuvable.");

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ResultT<Pattern>.Failure("Le nom du patron est invalide.");

            entity.Name = name;
            entity.Url = NormalizeOptional(request.Url);
            entity.Note = NormalizeOptional(request.Note);
            entity.Type = request.Type;
            entity.IsPersonal = request.IsPersonal;
            entity.BeginDate = request.BeginDate;
            entity.EndDate = request.EndDate;

            await context.SaveChangesAsync();
            return ResultT<Pattern>.Ok(ApplyFileMetadata(entity.ToDomain()));
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Pattern>.Failure($"Impossible de mettre à jour le patron {request.Id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Pattern>.Failure($"Impossible de mettre à jour le patron {request.Id}: {ex.Message}");
        }
    }

    public async Task<Result> AddDocumentAsync(int patternId, Guid documentId)
    {
        try
        {
            var pattern = await context.Patterns
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.PatternId == patternId);
            if (pattern is null)
                return Result.NotFound($"Le patron {patternId} est introuvable.");

            var document = await context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (document is null)
                return Result.NotFound($"Le document {documentId} est introuvable.");

            if (pattern.Documents.Any(d => d.DocumentId == documentId))
                return Result.Ok();

            if (document.ProjectId.HasValue)
                return Result.Failure("Ce document est déjà lié à un projet.");

            document.PatternId = patternId;
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible d'ajouter le document au patron {patternId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible d'ajouter le document au patron {patternId}: {ex.Message}");
        }
    }

    public async Task<Result> RemoveDocumentAsync(int patternId, Guid documentId)
    {
        try
        {
            var pattern = await context.Patterns
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.PatternId == patternId);
            if (pattern is null)
                return Result.NotFound($"Le patron {patternId} est introuvable.");

            var document = pattern.Documents.FirstOrDefault(d => d.DocumentId == documentId);
            if (document is null)
                return Result.NotFound($"Le document {documentId} n'est pas lié à ce patron.");

            pattern.Documents.Remove(document);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible de retirer le document du patron {patternId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible de retirer le document du patron {patternId}: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            var entity = await context.Patterns
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.PatternId == id);
            if (entity is null)
                return Result.NotFound($"Le patron {id} est introuvable.");

            foreach (var document in entity.Documents)
            {
                DeleteDocumentFile(document.DocumentId);
            }

            context.Documents.RemoveRange(entity.Documents);
            context.Patterns.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible de supprimer le patron {id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible de supprimer le patron {id}: {ex.Message}");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void DeleteDocumentFile(Guid documentId)
    {
        var filePath = pathManager.GetDocumentStoragePath(documentId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private Pattern ApplyFileMetadata(Pattern pattern) =>
        new()
        {
            Id = pattern.Id,
            Name = pattern.Name,
            Url = pattern.Url,
            Note = pattern.Note,
            Documents = pattern.Documents.Select(ApplyFileMetadata).ToList(),
            Projects = pattern.Projects,
            IsPersonal = pattern.IsPersonal,
            Type = pattern.Type,
            BeginDate = pattern.BeginDate,
            EndDate = pattern.EndDate
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
                ProjectId = document.ProjectId,
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
            ProjectId = document.ProjectId,
        };
    }
}
