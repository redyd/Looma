// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Integrity;
using Looma.Domain.IServices;
using Looma.Domain.Localization;
using Looma.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Services;

/// <param name="contextFactory">
/// Creates a dedicated context per operation: checks run in the background and must not share
/// the long-lived context used by the screens.
/// </param>
public sealed class DataIntegrityService(
    Func<LoomaDbContext> contextFactory,
    AppPaths paths,
    AppConfigStore configStore) : IDataIntegrityService
{
    public async Task<ResultT<IntegrityReport>> CheckAsync()
    {
        try
        {
            var issues = new List<IntegrityIssue>();

            if (DatabaseHealth.Check(paths.DatabasePath, out var databaseError) == DatabaseState.Corrupt)
            {
                issues.Add(new IntegrityIssue(IntegrityIssueKind.DatabaseCorrupt, Path.GetFileName(paths.DatabasePath), databaseError));
                // Document checks need a readable database.
                return ResultT<IntegrityReport>.Ok(new IntegrityReport(issues));
            }

            if (configStore.RecoveredCorruptConfigPath is { } corruptConfig)
                issues.Add(new IntegrityIssue(IntegrityIssueKind.ConfigRecovered, Path.GetFileName(corruptConfig)));

            issues.AddRange(FindInvalidThemes());

            var documents = await LoadDocumentsAsync();
            issues.AddRange(documents
                .Where(document => !File.Exists(paths.GetDocumentStoragePath(document.Id)))
                .Select(document => new IntegrityIssue(IntegrityIssueKind.DocumentFileMissing, document.Id.ToString(), document.Nickname)));
            issues.AddRange(FindOrphanFiles(documents.Select(document => document.Id).ToHashSet())
                .Select(path => new IntegrityIssue(IntegrityIssueKind.DocumentFileOrphan, Path.GetFileName(path))));

            return ResultT<IntegrityReport>.Ok(new IntegrityReport(issues));
        }
        catch (Exception ex)
        {
            return ResultT<IntegrityReport>.Failure(Localizer.Format("Data_Errors_UnableToCheckIntegrity", ex.Message));
        }
    }

    public async Task<ResultT<int>> QuarantineOrphanFilesAsync()
    {
        try
        {
            var documents = await LoadDocumentsAsync();
            var orphans = FindOrphanFiles(documents.Select(document => document.Id).ToHashSet());
            if (orphans.Count == 0)
                return ResultT<int>.Ok(0);

            Directory.CreateDirectory(paths.OrphanDocumentsFolder);
            foreach (var orphan in orphans)
            {
                var destination = Path.Combine(paths.OrphanDocumentsFolder, Path.GetFileName(orphan));
                if (File.Exists(destination))
                    destination = Path.Combine(paths.OrphanDocumentsFolder, $"{DateTime.Now:yyyyMMddHHmmss}-{Path.GetFileName(orphan)}");

                File.Move(orphan, destination);
            }

            return ResultT<int>.Ok(orphans.Count);
        }
        catch (Exception ex)
        {
            return ResultT<int>.Failure(Localizer.Format("Data_Errors_UnableToQuarantineOrphans", ex.Message));
        }
    }

    public async Task<ResultT<int>> RemoveMissingDocumentEntriesAsync()
    {
        try
        {
            await using var context = contextFactory();
            var entities = await context.Documents.ToListAsync();
            var missing = entities
                .Where(document => !File.Exists(paths.GetDocumentStoragePath(document.DocumentId)))
                .ToList();

            if (missing.Count == 0)
                return ResultT<int>.Ok(0);

            context.Documents.RemoveRange(missing);
            await context.SaveChangesAsync();
            return ResultT<int>.Ok(missing.Count);
        }
        catch (Exception ex)
        {
            return ResultT<int>.Failure(Localizer.Format("Data_Errors_UnableToRemoveMissingDocuments", ex.Message));
        }
    }

    private async Task<List<(Guid Id, string Nickname)>> LoadDocumentsAsync()
    {
        await using var context = contextFactory();
        var rows = await context.Documents
            .AsNoTracking()
            .Select(document => new { document.DocumentId, document.Nickname })
            .ToListAsync();

        return rows.Select(row => (row.DocumentId, row.Nickname)).ToList();
    }

    private IEnumerable<IntegrityIssue> FindInvalidThemes()
    {
        if (!Directory.Exists(paths.ThemesFolder))
            return [];

        return Directory
            .EnumerateFiles(paths.ThemesFolder, "*.json", SearchOption.TopDirectoryOnly)
            .Where(path => !AppConfigStore.IsValidJson(path))
            .Select(path => new IntegrityIssue(IntegrityIssueKind.ThemeInvalid, Path.GetFileName(path)));
    }

    private List<string> FindOrphanFiles(HashSet<Guid> knownDocumentIds)
    {
        if (!Directory.Exists(paths.DocumentsFolder))
            return [];

        // Stored files are named "{documentId}" or "{documentId}.{ext}"; the quarantine sub-folder is not scanned.
        return Directory
            .EnumerateFiles(paths.DocumentsFolder, "*", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                var dot = name.IndexOf('.');
                var idPart = dot < 0 ? name : name[..dot];
                return !Guid.TryParse(idPart, out var id) || !knownDocumentIds.Contains(id)
                       || !string.Equals(paths.GetDocumentStoragePath(id), path, StringComparison.Ordinal);
            })
            .ToList();
    }
}
