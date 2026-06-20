// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Storage;

namespace Looma.Infrastructure.Tests;

public sealed class ThemeStorageTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        "looma-theme-storage-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void SeedThemeFiles_CopiesMissingJsonFiles()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        var sourceFolder = CreateSeedFolder(("looma.json", """{"Name":"Looma"}"""));
        var storage = new ThemeStorage(paths);

        var copied = storage.SeedThemeFiles(sourceFolder);

        Assert.Equal(1, copied);
        Assert.True(File.Exists(Path.Combine(paths.ThemesFolder, "looma.json")));
    }

    [Fact]
    public void SeedThemeFiles_DoesNotOverwriteExistingTheme()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        var destinationPath = Path.Combine(paths.ThemesFolder, "looma.json");
        File.WriteAllText(destinationPath, """{"Name":"User theme"}""");
        var sourceFolder = CreateSeedFolder(("looma.json", """{"Name":"Seed theme"}"""));
        var storage = new ThemeStorage(paths);

        var copied = storage.SeedThemeFiles(sourceFolder);

        Assert.Equal(0, copied);
        Assert.Equal("""{"Name":"User theme"}""", File.ReadAllText(destinationPath));
    }

    [Fact]
    public void SeedThemeFiles_IgnoresNonJsonFiles()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        var sourceFolder = CreateSeedFolder(("ignored.txt", "not a theme"));
        var storage = new ThemeStorage(paths);

        var copied = storage.SeedThemeFiles(sourceFolder);

        Assert.Equal(0, copied);
        Assert.Empty(Directory.EnumerateFiles(paths.ThemesFolder));
    }

    [Fact]
    public void DeleteTheme_RemovesThemeFile()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        var themePath = Path.Combine(paths.ThemesFolder, "looma.json");
        File.WriteAllText(themePath, """{"Name":"Theme"}""");
        var storage = new ThemeStorage(paths);

        storage.DeleteTheme(themePath);

        Assert.False(File.Exists(themePath));
    }

    [Fact]
    public void DeleteTheme_ClearsSelectedThemeWhenDeleted()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        var themePath = Path.Combine(paths.ThemesFolder, "looma.json");
        File.WriteAllText(themePath, """{"Name":"Theme"}""");
        var storage = new ThemeStorage(paths);
        storage.SaveSelectedTheme(themePath);

        storage.DeleteTheme(themePath);

        Assert.Null(storage.GetSelectedThemePath());
    }

    [Fact]
    public void GetSelectedThemePath_WhenConfigJsonIsInvalid_ThrowsClearError()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        File.WriteAllText(paths.ConfigPath, """{"SelectedTheme":""");
        var storage = new ThemeStorage(paths);

        var exception = Assert.Throws<InvalidOperationException>(() => storage.GetSelectedThemePath());

        Assert.Contains("Le fichier de configuration \"config.json\" contient un JSON invalide.", exception.Message);
        Assert.Contains("Vérifiez la syntaxe du fichier.", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private string CreateSeedFolder(params (string FileName, string Content)[] files)
    {
        var sourceFolder = Path.Combine(_rootPath, "seed-themes");
        Directory.CreateDirectory(sourceFolder);

        foreach (var (fileName, content) in files)
        {
            File.WriteAllText(Path.Combine(sourceFolder, fileName), content);
        }

        return sourceFolder;
    }
}
