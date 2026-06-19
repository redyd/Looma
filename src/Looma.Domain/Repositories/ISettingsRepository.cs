// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

namespace Looma.Domain.Repositories;

public interface ISettingsRepository
{
    Task<string?> GetVersionAsync();
    Task SetVersionAsync(string version);
    Task<string?> GetReleaseNotesAsync(string version);
    Task SetReleaseNotesAsync(string version, string releaseNotes);
    Task<bool> GetReleaseNotesShownAsync(string version);
    Task SetReleaseNotesShownAsync(string version, bool shown);
}
