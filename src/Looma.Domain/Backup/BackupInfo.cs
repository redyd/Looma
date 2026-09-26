// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Backup;

/// <summary>An automatic backup stored in the application backups folder.</summary>
public sealed record BackupInfo(string Path, string Reason, DateTime CreatedAt, long Size);
