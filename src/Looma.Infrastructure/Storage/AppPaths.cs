// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Storage;

public class AppPaths(string baseRoot)
{
    public string DatabasePath => Path.Combine(baseRoot, "looma.db");
    public string ConfigPath => Path.Combine(baseRoot, "config.json");
    public string DocumentsFolder => Path.Combine(baseRoot, "documents");
    public string ThemesFolder => Path.Combine(baseRoot, "themes");
    public string BackupsFolder => Path.Combine(baseRoot, "backups");
    public string PendingRestoreFolder => Path.Combine(baseRoot, "pending-restore");
    public string OrphanDocumentsFolder => Path.Combine(DocumentsFolder, ".orphans");
    public string RootPath => baseRoot;

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(baseRoot);
        Directory.CreateDirectory(DocumentsFolder);
        Directory.CreateDirectory(ThemesFolder);
        Directory.CreateDirectory(BackupsFolder);
    }

    public void EnsureDatabaseCreated(LoomaDbContext context)
    {
        context.Database.Migrate();
    }

    public void ClearDocuments()
    {
        if (Directory.Exists(DocumentsFolder))
        {
            Directory.Delete(DocumentsFolder, recursive: true);
        }

        Directory.CreateDirectory(DocumentsFolder);
    }

    public string GetDocumentStoragePath(Guid id)
    {
        var exact = Path.Combine(DocumentsFolder, id.ToString());
        if (File.Exists(exact))
            return exact;

        var match = Directory.EnumerateFiles(DocumentsFolder, $"{id}.*")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return match ?? exact;
    }

    /// <summary>
    /// Deletes the stored file of a document. Call only after the database change is committed:
    /// a leftover file is recoverable (orphan), a dangling database row pointing to nothing is not.
    /// </summary>
    public bool TryDeleteDocumentFile(Guid id)
    {
        try
        {
            var filePath = GetDocumentStoragePath(id);
            if (File.Exists(filePath))
                File.Delete(filePath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static string BuildDocumentFileName(Guid id, string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        return string.IsNullOrWhiteSpace(extension)
            ? id.ToString()
            : $"{id}{extension}";
    }
}
