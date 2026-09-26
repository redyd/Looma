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
using Looma.Domain.Localization;

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
            return ResultT<IReadOnlyList<Pattern>>.Failure(Localizer.Format("Errors_UnableToLoadPatterns", ex.Message));
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
                ? ResultT<Pattern>.NotFound(Localizer.Format("Errors_PatternNotFound", id))
                : ResultT<Pattern>.Ok(ApplyFileMetadata(entity.ToDomain()));
        }
        catch (Exception ex)
        {
            return ResultT<Pattern>.Failure(Localizer.Format("Errors_UnableToLoadPattern", id, ex.Message));
        }
    }

    public async Task<ResultT<Pattern>> AddAsync(CreatePatternRequest request)
    {
        try
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ResultT<Pattern>.Failure(Localizer.Get("Errors_InvalidPatternName"));

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
            return ResultT<Pattern>.Failure(Localizer.Format("Errors_UnableToAddPattern", ex.Message));
        }
        catch (Exception ex)
        {
            return ResultT<Pattern>.Failure(Localizer.Format("Errors_UnableToAddPattern", ex.Message));
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
                return ResultT<Pattern>.NotFound(Localizer.Format("Errors_PatternNotFound", request.Id));

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ResultT<Pattern>.Failure(Localizer.Get("Errors_InvalidPatternName"));

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
            return ResultT<Pattern>.Failure(Localizer.Format("Errors_UnableToUpdatePattern", request.Id, ex.Message));
        }
        catch (Exception ex)
        {
            return ResultT<Pattern>.Failure(Localizer.Format("Errors_UnableToUpdatePattern", request.Id, ex.Message));
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
                return Result.NotFound(Localizer.Format("Errors_PatternNotFound", patternId));

            var document = await context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (document is null)
                return Result.NotFound(Localizer.Format("Errors_DocumentNotFound", documentId));

            if (pattern.Documents.Any(d => d.DocumentId == documentId))
                return Result.Ok();

            if (document.ProjectId.HasValue)
                return Result.Failure(Localizer.Get("Errors_DocumentAlreadyLinkedToProject"));

            document.PatternId = patternId;
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(Localizer.Format("Errors_UnableToLinkDocumentToPattern", patternId, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure(Localizer.Format("Errors_UnableToLinkDocumentToPattern", patternId, ex.Message));
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
                return Result.NotFound(Localizer.Format("Errors_PatternNotFound", patternId));

            var document = pattern.Documents.FirstOrDefault(d => d.DocumentId == documentId);
            if (document is null)
                return Result.NotFound(Localizer.Format("Errors_DocumentNotLinkedToPattern", documentId));

            pattern.Documents.Remove(document);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(Localizer.Format("Errors_UnableToUnlinkDocumentFromPattern", patternId, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure(Localizer.Format("Errors_UnableToUnlinkDocumentFromPattern", patternId, ex.Message));
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
                return Result.NotFound(Localizer.Format("Errors_PatternNotFound", id));

            var documentIds = entity.Documents.Select(d => d.DocumentId).ToList();
            context.Documents.RemoveRange(entity.Documents);
            context.Patterns.Remove(entity);
            await context.SaveChangesAsync();

            foreach (var documentId in documentIds)
                pathManager.TryDeleteDocumentFile(documentId);

            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(Localizer.Format("Errors_UnableToDeletePattern", id, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure(Localizer.Format("Errors_UnableToDeletePattern", id, ex.Message));
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();


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
