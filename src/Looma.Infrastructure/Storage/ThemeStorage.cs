// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Services;

namespace Looma.Infrastructure.Storage;

public sealed class ThemeStorage(AppPaths paths) : IThemeStorage
{
    public IReadOnlyList<string> GetThemeFiles()
    {
        Directory.CreateDirectory(paths.ThemesFolder);

        return Directory
            .EnumerateFiles(paths.ThemesFolder, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
}
