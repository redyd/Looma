// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Looma.Infrastructure.Storage;

/// <summary>
/// Single owner of config.json. Reads tolerate a corrupted file (it is set aside and
/// defaults are used) and writes are atomic and serialized.
/// </summary>
public sealed class AppConfigStore(AppPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly Lock _gate = new();

    /// <summary>Path of the corrupted config set aside during this session, if any.</summary>
    public string? RecoveredCorruptConfigPath { get; private set; }

    public AppConfig Read()
    {
        lock (_gate)
        {
            return ReadUnsafe();
        }
    }

    public void Update(Action<AppConfig> update)
    {
        lock (_gate)
        {
            var config = ReadUnsafe();
            update(config);
            AtomicFile.WriteAllText(paths.ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
        }
    }

    public static bool IsValidJson(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var _ = JsonDocument.Parse(stream);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private AppConfig ReadUnsafe()
    {
        if (!File.Exists(paths.ConfigPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(paths.ConfigPath);
            if (string.IsNullOrWhiteSpace(json))
                return new AppConfig();

            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            config.ReleaseNotes ??= [];
            return config;
        }
        catch (JsonException)
        {
            SetAsideCorruptConfig();
            return new AppConfig();
        }
    }

    private void SetAsideCorruptConfig()
    {
        var corruptPath = $"{paths.ConfigPath}.corrupt-{DateTime.Now:yyyyMMddHHmmss}";
        try
        {
            File.Move(paths.ConfigPath, corruptPath, overwrite: true);
            RecoveredCorruptConfigPath = corruptPath;
        }
        catch (IOException)
        {
            // The corrupted file stays in place; the next successful write replaces it atomically.
            RecoveredCorruptConfigPath = paths.ConfigPath;
        }
    }
}

public sealed class AppConfig
{
    public string? SelectedTheme { get; set; }
    public string? SelectedLanguage { get; set; }
    public string? Version { get; set; }
    public Dictionary<string, ReleaseNoteConfig> ReleaseNotes { get; set; } = [];
}

public sealed class ReleaseNoteConfig
{
    public string Markdown { get; set; } = string.Empty;
    public bool Shown { get; set; }
}
