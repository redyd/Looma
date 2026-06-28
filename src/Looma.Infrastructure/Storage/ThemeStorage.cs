// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Text.Json;
using System.Text.Json.Serialization;
using Looma.Domain.Services;

namespace Looma.Infrastructure.Storage;

public sealed class ThemeStorage(AppPaths paths) : IThemeStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public IReadOnlyList<string> GetThemeFiles()
    {
        Directory.CreateDirectory(paths.ThemesFolder);

        return Directory
            .EnumerateFiles(paths.ThemesFolder, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? GetSelectedThemePath()
    {
        var config = ReadConfig();
        if (string.IsNullOrWhiteSpace(config.SelectedTheme))
            return null;

        var fileName = Path.GetFileName(config.SelectedTheme);
        var themePath = Path.Combine(paths.ThemesFolder, fileName);

        return File.Exists(themePath)
            ? themePath
            : null;
    }

    public int SeedThemeFiles(string sourceFolder)
    {
        if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
            return 0;

        Directory.CreateDirectory(paths.ThemesFolder);

        var copied = 0;
        foreach (var sourcePath in Directory.EnumerateFiles(sourceFolder, "*.json", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(sourcePath);
            var destinationPath = Path.Combine(paths.ThemesFolder, fileName);

            if (File.Exists(destinationPath))
                continue;

            File.Copy(sourcePath, destinationPath, overwrite: false);
            copied++;
        }

        return copied;
    }

    public void SaveSelectedTheme(string? themePath)
    {
        var selectedTheme = string.IsNullOrWhiteSpace(themePath)
            ? null
            : Path.GetFileName(themePath);

        var config = ReadConfig();
        config.SelectedTheme = selectedTheme;

        var directory = Path.GetDirectoryName(paths.ConfigPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(paths.ConfigPath, json);
    }

    public void DeleteTheme(string themePath)
    {
        if (string.IsNullOrWhiteSpace(themePath))
            throw new ArgumentException("Le chemin du thème est requis.", nameof(themePath));

        var fileName = Path.GetFileName(themePath);
        var destinationPath = Path.Combine(paths.ThemesFolder, fileName);

        if (!File.Exists(destinationPath))
            throw new FileNotFoundException("Le fichier de thème est introuvable.", destinationPath);

        File.Delete(destinationPath);

        var config = ReadConfig();
        if (string.Equals(config.SelectedTheme, fileName, StringComparison.OrdinalIgnoreCase))
        {
            SaveSelectedTheme(null);
        }
    }

    public string ImportTheme(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Le fichier de thème est introuvable.", sourcePath);

        if (!Path.GetExtension(sourcePath).Equals(".json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Le thème doit être un fichier JSON.");

        Directory.CreateDirectory(paths.ThemesFolder);

        var fileName = Path.GetFileName(sourcePath);
        var destinationPath = Path.Combine(paths.ThemesFolder, fileName);

        if (File.Exists(destinationPath))
        {
            destinationPath = BuildAvailableThemePath(fileName);
        }

        File.Copy(sourcePath, destinationPath);
        return destinationPath;
    }

    public string CreateExportPath()
    {
        var downloadsFolder = GetDownloadsFolder();
        Directory.CreateDirectory(downloadsFolder);

        var fileName = $"looma-theme-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        return Path.Combine(downloadsFolder, fileName);
    }

    private string BuildAvailableThemePath(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var index = 1; index < int.MaxValue; index++)
        {
            var candidate = Path.Combine(paths.ThemesFolder, $"{baseName}-{index}{extension}");
            if (!File.Exists(candidate))
                return candidate;
        }

        throw new InvalidOperationException("Impossible de trouver un nom disponible pour ce thème.");
    }

    private static string GetDownloadsFolder()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(userProfile)
            ? Environment.CurrentDirectory
            : Path.Combine(userProfile, "Downloads");
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
