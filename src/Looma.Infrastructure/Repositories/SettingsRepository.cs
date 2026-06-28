// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Text.Json;
using System.Text.Json.Serialization;
using Looma.Domain.Repositories;
using Looma.Infrastructure.Storage;

namespace Looma.Infrastructure.Repositories;

public sealed class SettingsRepository(AppPaths paths) : ISettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public Task<string?> GetSelectedLanguageAsync()
    {
        var config = ReadConfig();
        return Task.FromResult(config.SelectedLanguage);
    }

    public Task SetSelectedLanguageAsync(string culture)
    {
        var config = ReadConfig();
        config.SelectedLanguage = culture;
        WriteConfig(config);
        return Task.CompletedTask;
    }

    public Task<string?> GetVersionAsync()
    {
        var config = ReadConfig();
        return Task.FromResult(config.Version);
    }

    public Task SetVersionAsync(string version)
    {
        var config = ReadConfig();
        config.Version = version;
        WriteConfig(config);
        return Task.CompletedTask;
    }

    public Task<string?> GetReleaseNotesAsync(string version)
    {
        var config = ReadConfig();
        return Task.FromResult(
            config.ReleaseNotes.TryGetValue(version, out var release)
                ? release.Markdown
                : null);
    }

    public Task SetReleaseNotesAsync(string version, string releaseNotes)
    {
        var config = ReadConfig();
        var release = GetOrCreateRelease(config, version);
        release.Markdown = releaseNotes;
        WriteConfig(config);
        return Task.CompletedTask;
    }

    public Task<bool> GetReleaseNotesShownAsync(string version)
    {
        var config = ReadConfig();
        return Task.FromResult(
            config.ReleaseNotes.TryGetValue(version, out var release)
            && release.Shown);
    }

    public Task SetReleaseNotesShownAsync(string version, bool shown)
    {
        var config = ReadConfig();
        var release = GetOrCreateRelease(config, version);
        release.Shown = shown;
        WriteConfig(config);
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

    private AppConfig ReadConfig()
    {
        if (!File.Exists(paths.ConfigPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(paths.ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                BuildConfigJsonErrorMessage(ex),
                ex);
        }
    }

    private string BuildConfigJsonErrorMessage(JsonException ex)
    {
        var location = ex.LineNumber is null || ex.BytePositionInLine is null
            ? string.Empty
            : $" Ligne {ex.LineNumber + 1}, colonne {ex.BytePositionInLine + 1}.";

        return $"Le fichier de configuration \"{Path.GetFileName(paths.ConfigPath)}\" contient un JSON invalide.{location} Vérifiez la syntaxe du fichier.";
    }

    private void WriteConfig(AppConfig config)
    {
        var directory = Path.GetDirectoryName(paths.ConfigPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(paths.ConfigPath, json);
    }

    private sealed class AppConfig
    {
        public string? SelectedTheme { get; set; }
        public string? SelectedLanguage { get; set; }
        public string? Version { get; set; }
        public Dictionary<string, ReleaseNoteConfig> ReleaseNotes { get; set; } = [];
    }

    private sealed class ReleaseNoteConfig
    {
        public string Markdown { get; set; } = string.Empty;
        public bool Shown { get; set; }
    }
}
