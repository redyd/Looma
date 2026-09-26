// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Services;
using Looma.Domain.Localization;

namespace Looma.Infrastructure.Storage;

public sealed class ThemeStorage(AppPaths paths, AppConfigStore configStore) : IThemeStorage
{
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
        var config = configStore.Read();
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

            if (!AppConfigStore.IsValidJson(sourcePath))
                continue;

            if (File.Exists(destinationPath))
            {
                if (AppConfigStore.IsValidJson(destinationPath))
                    continue;

                // A corrupted built-in theme is set aside, never silently discarded.
                File.Move(destinationPath, $"{destinationPath}.corrupt-{DateTime.Now:yyyyMMddHHmmss}", overwrite: true);
            }

            AtomicFile.Copy(sourcePath, destinationPath);
            copied++;
        }

        return copied;
    }

    public void SaveSelectedTheme(string? themePath)
    {
        var selectedTheme = string.IsNullOrWhiteSpace(themePath)
            ? null
            : Path.GetFileName(themePath);

        configStore.Update(config => config.SelectedTheme = selectedTheme);
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

        var config = configStore.Read();
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

        if (!AppConfigStore.IsValidJson(sourcePath))
            throw new InvalidOperationException(Localizer.Get("Data_Errors_InvalidThemeJson"));

        Directory.CreateDirectory(paths.ThemesFolder);

        var fileName = Path.GetFileName(sourcePath);
        var destinationPath = Path.Combine(paths.ThemesFolder, fileName);

        if (File.Exists(destinationPath))
        {
            destinationPath = BuildAvailableThemePath(fileName);
        }

        AtomicFile.Copy(sourcePath, destinationPath);
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
}
