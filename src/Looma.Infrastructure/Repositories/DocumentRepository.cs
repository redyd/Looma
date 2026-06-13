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

namespace Looma.Infrastructure.Repositories;

public class DocumentRepository(LoomaDbContext context, AppPaths pathManager) : IDocumentRepository
{
    public async Task<ResultT<IReadOnlyList<Document>>> GetAllAsync()
    {
        try
        {
            var entities = await context.Documents
                .AsNoTracking()
                .OrderBy(d => d.Nickname)
                .ThenBy(d => d.DocumentId)
                .ToListAsync();

            var documents = entities
                .Select(e => e.ToDomain())
                .Select(e => ApplyFileMetadata(e, pathManager))
                .ToList();

            return ResultT<IReadOnlyList<Document>>.Ok(documents);
        }
        catch (Exception ex)
        {
            return ResultT<IReadOnlyList<Document>>.Failure($"Impossible de charger les documents: {ex.Message}");
        }
    }

    public async Task<ResultT<Document>> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            return entity is null
                ? ResultT<Document>.NotFound($"Le document {id} est introuvable.")
                : ResultT<Document>.Ok(ApplyFileMetadata(entity.ToDomain(), pathManager));
        }
        catch (Exception ex)
        {
            return ResultT<Document>.Failure($"Impossible de charger le document {id}: {ex.Message}");
        }
    }

    public async Task<ResultT<Document>> AddAsync(CreateDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourcePath))
            return ResultT<Document>.Failure("Le chemin source du document est invalide.");

        if (!File.Exists(request.SourcePath))
            return ResultT<Document>.NotFound($"Le fichier source \"{request.SourcePath}\" est introuvable.");

        var id = Guid.NewGuid();
        var nickname = string.IsNullOrWhiteSpace(request.Nickname)
            ? Path.GetFileNameWithoutExtension(request.SourcePath)
            : request.Nickname.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
            return ResultT<Document>.Failure("Le nom affiché du document est invalide.");

        var destinationFileName = AppPaths.BuildDocumentFileName(id, request.SourcePath);
        var destinationPath = Path.Combine(pathManager.DocumentsFolder, destinationFileName);

        try
        {
            if (request.PatternId.HasValue && request.ProjectId.HasValue)
                return ResultT<Document>.Failure("Un document ne peut être lié qu'à un patron ou à un projet.");

            if (request.PatternId.HasValue)
            {
                var patternExists = await context.Patterns.AnyAsync(p => p.PatternId == request.PatternId.Value);
                if (!patternExists)
                    return ResultT<Document>.NotFound($"Le patron {request.PatternId.Value} est introuvable.");
            }

            if (request.ProjectId.HasValue)
            {
                var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == request.ProjectId.Value);
                if (!projectExists)
                    return ResultT<Document>.NotFound($"Le projet {request.ProjectId.Value} est introuvable.");
            }

            Directory.CreateDirectory(pathManager.DocumentsFolder);
            File.Copy(request.SourcePath, destinationPath, overwrite: false);

            var fileInfo = new FileInfo(destinationPath);
            var entity = new DocumentEntity
            {
                DocumentId = id,
                Nickname = nickname,
                Type = GetDocumentType(destinationPath),
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
                File.Delete(destinationPath);

            return ResultT<Document>.Failure($"Impossible d'ajouter le document: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            return ResultT<Document>.Failure($"Impossible d'ajouter le document: {ex.Message}");
        }
    }

    public async Task<ResultT<Document>> UpdateAsync(UpdateDocumentRequest request)
    {
        try
        {
            var entity = await context.Documents.FirstOrDefaultAsync(d => d.DocumentId == request.Id);
            if (entity is null)
                return ResultT<Document>.NotFound($"Le document {request.Id} est introuvable.");

            var nickname = request.Nickname.Trim();
            if (string.IsNullOrWhiteSpace(nickname))
                return ResultT<Document>.Failure("Le nom du document ne peut pas être vide.");

            entity.Nickname = nickname;
            await context.SaveChangesAsync();

            return ResultT<Document>.Ok(ApplyFileMetadata(entity.ToDomain(), pathManager));
        }
        catch (DbUpdateException ex)
        {
            return ResultT<Document>.Failure($"Impossible de mettre à jour le document {request.Id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultT<Document>.Failure($"Impossible de mettre à jour le document {request.Id}: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await context.Documents.FindAsync(id);
            if (entity is null)
                return Result.NotFound($"Le document {id} est introuvable.");

            var filePath = pathManager.GetDocumentStoragePath(id);
            if (File.Exists(filePath))
                File.Delete(filePath);

            context.Documents.Remove(entity);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure($"Impossible de supprimer le document {id}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Impossible de supprimer le document {id}: {ex.Message}");
        }
    }

    public Task<Result> OpenAsync(Guid id)
    {
        try
        {
            var filePath = pathManager.GetDocumentStoragePath(id);
            if (!File.Exists(filePath))
                return Task.FromResult(Result.NotFound($"Le fichier du document {id} est introuvable."));

            Process.Start(new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            });

            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Impossible d'ouvrir le document {id}: {ex.Message}"));
        }
    }

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
                StoragePath = null
            };
        }

        var info = new FileInfo(filePath);
        return new Document
        {
            Id = document.Id,
            Nickname = document.Nickname,
            Type = GetDocumentType(filePath),
            SizeBytes = info.Length,
            StoragePath = filePath
        };
    }

    private static string GetDocumentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).TrimStart('.');
        return string.IsNullOrWhiteSpace(extension)
            ? "Sans extension"
            : extension.ToUpperInvariant();
    }
}
