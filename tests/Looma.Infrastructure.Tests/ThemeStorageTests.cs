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
        var storage = new ThemeStorage(paths, new AppConfigStore(paths));

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
        var storage = new ThemeStorage(paths, new AppConfigStore(paths));

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
        var storage = new ThemeStorage(paths, new AppConfigStore(paths));

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
        var storage = new ThemeStorage(paths, new AppConfigStore(paths));

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
        var storage = new ThemeStorage(paths, new AppConfigStore(paths));
        storage.SaveSelectedTheme(themePath);

        storage.DeleteTheme(themePath);

        Assert.Null(storage.GetSelectedThemePath());
    }

    [Fact]
    public void SaveSelectedTheme_PreservesSelectedLanguage()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        var themePath = Path.Combine(paths.ThemesFolder, "looma.json");
        File.WriteAllText(themePath, """{"Name":"Theme"}""");
        File.WriteAllText(paths.ConfigPath, """{"SelectedLanguage":"es"}""");
        var storage = new ThemeStorage(paths, new AppConfigStore(paths));

        storage.SaveSelectedTheme(themePath);

        var json = File.ReadAllText(paths.ConfigPath);
        Assert.Contains("\"SelectedLanguage\": \"es\"", json);
    }

    [Fact]
    public void GetSelectedThemePath_WhenConfigJsonIsInvalid_ReturnsNullAndSetsFileAside()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        File.WriteAllText(paths.ConfigPath, """{"SelectedTheme":""");
        var storage = new ThemeStorage(paths, new AppConfigStore(paths));

        var selected = storage.GetSelectedThemePath();

        Assert.Null(selected);
        Assert.Single(Directory.EnumerateFiles(_rootPath, "config.json.corrupt-*"));
    }

    [Fact]
    public void SeedThemeFiles_ReplacesCorruptedThemeAndKeepsCopy()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        var destinationPath = Path.Combine(paths.ThemesFolder, "looma.json");
        File.WriteAllText(destinationPath, """{"Name":""");
        var sourceFolder = CreateSeedFolder(("looma.json", """{"Name":"Seed theme"}"""));
        var storage = new ThemeStorage(paths, new AppConfigStore(paths));

        var copied = storage.SeedThemeFiles(sourceFolder);

        Assert.Equal(1, copied);
        Assert.Equal("""{"Name":"Seed theme"}""", File.ReadAllText(destinationPath));
        Assert.Single(Directory.EnumerateFiles(paths.ThemesFolder, "looma.json.corrupt-*"));
    }

    [Fact]
    public void ImportTheme_RejectsInvalidJson()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        var sourceFolder = CreateSeedFolder(("broken.json", "{not json"));
        var storage = new ThemeStorage(paths, new AppConfigStore(paths));

        Assert.Throws<InvalidOperationException>(() => storage.ImportTheme(Path.Combine(sourceFolder, "broken.json")));
        Assert.Empty(Directory.EnumerateFiles(paths.ThemesFolder));
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
