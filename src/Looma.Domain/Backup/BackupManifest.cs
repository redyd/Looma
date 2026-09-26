// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Backup;

public sealed record BackupManifest
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; init; } = CurrentFormatVersion;
    public string? AppVersion { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string? LastMigration { get; init; }
    public IReadOnlyList<BackupFileEntry> Files { get; init; } = [];

    public int DocumentCount => Files.Count(file => file.Path.StartsWith("documents/", StringComparison.Ordinal));
    public int ThemeCount => Files.Count(file => file.Path.StartsWith("themes/", StringComparison.Ordinal));
    public long TotalSize => Files.Sum(file => file.Size);
}

public sealed record BackupFileEntry(string Path, long Size, string Sha256);
