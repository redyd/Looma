// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Storage;

public class AppPaths(string baseRoot)
{
    public string DatabasePath => Path.Combine(baseRoot, "looma.db");
    public string DocumentsFolder => Path.Combine(baseRoot, "documents");

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(baseRoot);
        Directory.CreateDirectory(DocumentsFolder);
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

    public static string BuildDocumentFileName(Guid id, string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        return string.IsNullOrWhiteSpace(extension)
            ? id.ToString()
            : $"{id}{extension}";
    }
}
