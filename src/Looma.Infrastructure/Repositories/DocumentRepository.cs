// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Diagnostics;
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

public class DocumentRepository(LoomaDbContext context, AppPaths pathManager) : IDocumentRepository
{
    public async Task<ResultT<IReadOnlyList<Document>>> GetAllAsync()
    {
        try
        {
            var entities = await context.Documents
                .Include(d => d.Pattern)
                .Include(d => d.Project)
                .OrderBy(d => d.Nickname)
                .ThenBy(d => d.DocumentId)
                .ToListAsync();

            if (DocumentMetadataBackfill.Apply(entities, pathManager))
            {
                await context.SaveChangesAsync();
            }

            var documents = entities
                .Select(e => e.ToDomain())
                .Select(e => ApplyFileMetadata(e, pathManager))
                .ToList();

            return ResultT<IReadOnlyList<Document>>.Ok(documents);
        }
        catch (Exception ex)
        {
            return ResultT<IReadOnlyList<Document>>.Failure(Localizer.Format("Errors_UnableToLoadDocuments", ex.Message));
        }
    }

    public async Task<ResultT<Document>> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await context.Documents
                .Include(d => d.Pattern)
                .Include(d => d.Project)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (entity is not null && DocumentMetadataBackfill.Apply([entity], pathManager))
            {
                await context.SaveChangesAsync();
            }

            return entity is null
                ? ResultT<Document>.NotFound(Localizer.Format("Errors_DocumentNotFound", id))
                : ResultT<Document>.Ok(ApplyFileMetadata(entity.ToDomain(), pathManager));
        }
        catch (Exception ex)
        {
            return ResultT<Document>.Failure(Localizer.Format("Errors_UnableToLoadDocument", id, ex.Message));
        }
    }

    public async Task<ResultT<Document>> AddAsync(CreateDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourcePath))
        {
            return ResultT<Document>.Failure(Localizer.Get("Errors_InvalidDocumentSource"));
        }

        if (!File.Exists(request.SourcePath))
        {
            return ResultT<Document>.NotFound(Localizer.Format("Errors_SourceFileNotFound", request.SourcePath));
        }

        var id = Guid.NewGuid();
        var nickname = string.IsNullOrWhiteSpace(request.Nickname)
            ? Path.GetFileNameWithoutExtension(request.SourcePath)
            : request.Nickname.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
            return ResultT<Document>.Failure(Localizer.Get("Errors_InvalidDocumentName"));

        var destinationFileName = AppPaths.BuildDocumentFileName(id, request.SourcePath);
        var destinationPath = Path.Combine(pathManager.DocumentsFolder, destinationFileName);

        try
        {
            if (request is { PatternId: not null, ProjectId: not null })
            {
                return ResultT<Document>.Failure(Localizer.Get("Errors_DocumentSingleOwner"));
            }

            if (request.PatternId.HasValue)
            {
                var patternExists = await context.Patterns.AnyAsync(p => p.PatternId == request.PatternId.Value);
                if (!patternExists)
                {
                    return ResultT<Document>.NotFound(Localizer.Format("Errors_PatternNotFound", request.PatternId.Value));
                }
            }

            if (request.ProjectId.HasValue)
            {
                var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == request.ProjectId.Value);
                if (!projectExists)
                {
                    return ResultT<Document>.NotFound(Localizer.Format("Errors_ProjectNotFound", request.ProjectId.Value));
                }
            }

            Directory.CreateDirectory(pathManager.DocumentsFolder);
            if (File.Exists(destinationPath))
                return ResultT<Document>.Conflict(Localizer.Format("Data_Errors_DocumentStorageConflict", id));

            AtomicFile.Copy(request.SourcePath, destinationPath);

            var fileInfo = new FileInfo(destinationPath);
            var entity = new DocumentEntity
            {
                DocumentId = id,
                Nickname = nickname,
                Type = DocumentMetadataBackfill.GetDocumentType(destinationPath),
                Size = fileInfo.Length,
                PatternId = request.PatternId,
                ProjectId = request.ProjectId
            };

            context.Documents.Add(entity);
            await context.SaveChangesAsync();
            return ResultT<Document>.Ok(ApplyFileMetadata(entity.ToDomain(), pathManager));
        }
        catch (DbUpdateException ex)
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            return ResultT<Document>.Failure(Localizer.Format("Errors_UnableToAddDocument", ex.Message));
        }
        catch (Exception ex)
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            return ResultT<Document>.Failure(Localizer.Format("Errors_UnableToAddDocument", ex.Message));
        }
    }

    public async Task<ResultT<Document>> UpdateAsync(UpdateDocumentRequest request)
    {
        try
        {
            var entity = await context.Documents.FirstOrDefaultAsync(d => d.DocumentId == request.Id);
            if (entity is null)
            {
                return ResultT<Document>.NotFound(Localizer.Format("Errors_DocumentNotFound", request.Id));
            }

            var nickname = request.Nickname.Trim();
            if (string.IsNullOrWhiteSpace(nickname))
            {
                return ResultT<Document>.Failure(Localizer.Get("Errors_DocumentNameRequired"));
            }

            entity.Nickname = nickname;
            await context.SaveChangesAsync();

            return ResultT<Document>.Ok(ApplyFileMetadata(entity.ToDomain(), pathManager));
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Document>.Failure(Localizer.Format("Errors_UnableToUpdateDocument", request.Id, ex.Message));
        }
        catch (Exception ex)
        {
            return ResultT<Document>.Failure(Localizer.Format("Errors_UnableToUpdateDocument", request.Id, ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await context.Documents.FindAsync(id);
            if (entity is null)
            {
                return Result.NotFound(Localizer.Format("Errors_DocumentNotFound", id));
            }

            context.Documents.Remove(entity);
            await context.SaveChangesAsync();
            pathManager.TryDeleteDocumentFile(id);
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(Localizer.Format("Errors_UnableToDeleteDocument", id, ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure(Localizer.Format("Errors_UnableToDeleteDocument", id, ex.Message));
        }
    }

    public Task<Result> OpenAsync(Guid id)
    {
        try
        {
            var filePath = pathManager.GetDocumentStoragePath(id);
            if (!File.Exists(filePath))
            {
                return Task.FromResult(Result.NotFound(Localizer.Format("Errors_DocumentFileNotFound", id)));
            }

            Process.Start(new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            });

            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure(Localizer.Format("Errors_UnableToOpenDocument", id, ex.Message)));
        }
    }

    public void DiscardStoredFile(Guid id) => pathManager.TryDeleteDocumentFile(id);

    private static Document ApplyFileMetadata(Document document, AppPaths pathManager)
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
            Type = GetDocumentType(filePath),
            SizeBytes = info.Length,
            StoragePath = filePath,
            PatternId = document.PatternId,
            ProjectId = document.ProjectId,
        };
    }

    private static string GetDocumentType(string filePath) => DocumentMetadataBackfill.GetDocumentType(filePath);
}
