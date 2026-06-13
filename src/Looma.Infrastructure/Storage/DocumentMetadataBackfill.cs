// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Entity;

namespace Looma.Infrastructure.Storage;

internal static class DocumentMetadataBackfill
{
    public static bool Apply(IEnumerable<DocumentEntity> documents, AppPaths pathManager)
    {
        var changed = false;

        foreach (var document in documents)
        {
            if (!string.IsNullOrWhiteSpace(document.Type) && document.Size.HasValue)
                continue;

            var filePath = pathManager.GetDocumentStoragePath(document.DocumentId);
            if (!File.Exists(filePath))
            {
                document.Type ??= "Inconnu";
                document.Size ??= 0;
                changed = true;
                continue;
            }

            var info = new FileInfo(filePath);
            document.Type ??= GetDocumentType(filePath);
            document.Size ??= info.Length;
            changed = true;
        }

        return changed;
    }

    public static string GetDocumentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).TrimStart('.');
        return string.IsNullOrWhiteSpace(extension)
            ? "Sans extension"
            : extension.ToUpperInvariant();
    }
}
