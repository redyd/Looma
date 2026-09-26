// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Backup;
using Looma.Domain.Core;

namespace Looma.Domain.IServices;

public interface IBackupService
{
    string BackupsFolder { get; }

    /// <summary>Writes a complete backup archive (database snapshot, documents, themes, preferences).</summary>
    Task<ResultT<BackupManifest>> ExportAsync(string destinationPath);

    /// <summary>Fully validates an archive (structure, paths, checksums, database) without modifying any data.</summary>
    Task<ResultT<BackupManifest>> ValidateAsync(string archivePath);

    /// <summary>
    /// Validates and extracts an archive so it replaces all current data at the next start.
    /// Current data is backed up automatically before being replaced.
    /// </summary>
    Task<ResultT<BackupManifest>> StageRestoreAsync(string archivePath);

    IReadOnlyList<BackupInfo> ListAutomaticBackups();
}
