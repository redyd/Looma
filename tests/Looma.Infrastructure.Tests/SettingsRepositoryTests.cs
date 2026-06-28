// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Repositories;
using Looma.Infrastructure.Storage;

namespace Looma.Infrastructure.Tests;

public sealed class SettingsRepositoryTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        "looma-settings-repository-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetSelectedLanguageAsync_WhenConfigDoesNotExist_ReturnsNull()
    {
        var paths = new AppPaths(_rootPath);
        var repository = new SettingsRepository(paths);

        var language = await repository.GetSelectedLanguageAsync();

        Assert.Null(language);
    }

    [Fact]
    public async Task GetSelectedLanguageAsync_WhenConfigIsEmpty_ReturnsNull()
    {
        var paths = new AppPaths(_rootPath);
        paths.EnsureDirectoriesExist();
        File.WriteAllText(paths.ConfigPath, "{}");
        var repository = new SettingsRepository(paths);

        var language = await repository.GetSelectedLanguageAsync();

        Assert.Null(language);
    }

    [Fact]
    public async Task SetSelectedLanguageAsync_WritesSelectedLanguageToConfig()
    {
        var paths = new AppPaths(_rootPath);
        var repository = new SettingsRepository(paths);

        await repository.SetSelectedLanguageAsync("nl");

        var json = await File.ReadAllTextAsync(paths.ConfigPath);
        Assert.Contains("\"SelectedLanguage\": \"nl\"", json);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
