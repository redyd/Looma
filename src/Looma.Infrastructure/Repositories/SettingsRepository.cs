// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Repositories;
using Looma.Infrastructure.Storage;

namespace Looma.Infrastructure.Repositories;

public sealed class SettingsRepository(AppConfigStore configStore) : ISettingsRepository
{
    public Task<string?> GetSelectedLanguageAsync() =>
        Task.FromResult(configStore.Read().SelectedLanguage);

    public Task SetSelectedLanguageAsync(string culture)
    {
        configStore.Update(config => config.SelectedLanguage = culture);
        return Task.CompletedTask;
    }

    public Task<string?> GetVersionAsync() =>
        Task.FromResult(configStore.Read().Version);

    public Task SetVersionAsync(string version)
    {
        configStore.Update(config => config.Version = version);
        return Task.CompletedTask;
    }

    public Task<string?> GetReleaseNotesAsync(string version)
    {
        var config = configStore.Read();
        return Task.FromResult(
            config.ReleaseNotes.TryGetValue(version, out var release)
                ? release.Markdown
                : null);
    }

    public Task SetReleaseNotesAsync(string version, string releaseNotes)
    {
        configStore.Update(config => GetOrCreateRelease(config, version).Markdown = releaseNotes);
        return Task.CompletedTask;
    }

    public Task<bool> GetReleaseNotesShownAsync(string version)
    {
        var config = configStore.Read();
        return Task.FromResult(
            config.ReleaseNotes.TryGetValue(version, out var release)
            && release.Shown);
    }

    public Task SetReleaseNotesShownAsync(string version, bool shown)
    {
        configStore.Update(config => GetOrCreateRelease(config, version).Shown = shown);
        return Task.CompletedTask;
    }

    private static ReleaseNoteConfig GetOrCreateRelease(AppConfig config, string version)
    {
        if (config.ReleaseNotes.TryGetValue(version, out var release))
            return release;

        release = new ReleaseNoteConfig();
        config.ReleaseNotes[version] = release;
        return release;
    }
}
