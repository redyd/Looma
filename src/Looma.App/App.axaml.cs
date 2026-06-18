// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Looma.App.Services;
using Looma.Infrastructure;
using Looma.Infrastructure.Storage;
using Looma.Domain.Services;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Main;
using Looma.Views.Views.Main;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Velopack;
using Velopack.Sources;

namespace Looma.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (Design.IsDesignMode)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        var services = new ServiceCollection();

        var rootPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Looma"
        );

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desk)
        {
            var startupArgs = desk.Args;
            if (startupArgs?.Contains("--local") == true)
            {
                rootPath = Path.GetFullPath(Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Data"
                ));
            }
        }

        services.AddPresentation();
        services.AddInfrastructure();
        services.AddDomain();
        services.AddSingleton<AppPaths>(_ => new AppPaths(rootPath));

        services.AddDbContext<LoomaDbContext>((sp, options) =>
            options.UseSqlite($"Data Source={sp.GetService<AppPaths>()?.DatabasePath}"));

        Services = services.BuildServiceProvider();

        var pathManager = Services.GetService<AppPaths>();

        if (pathManager is null)
        {
            throw new ArgumentException($"Could not find {nameof(AppPaths)}.");
        }

        pathManager.EnsureDirectoriesExist();

        using var scope = Services.CreateScope();
        var args = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Args ?? [];
        var db = scope.ServiceProvider.GetRequiredService<LoomaDbContext>();

        if (args.Contains("--clear"))
        {
            db.Database.EnsureDeleted();
            pathManager.ClearDocuments();
        }

        pathManager.EnsureDatabaseCreated(db);

        ApplyStoredTheme();

        var seedCount = GetSeedCount(args);
        if (seedCount.HasValue || args.Contains("--seed"))
        {
            scope.ServiceProvider.GetRequiredService<IAppDataSeeder>()
                .SeedAsync(seedCount)
                .GetAwaiter()
                .GetResult();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var channel = OperatingSystem.IsWindows() ? "win" : "linux";

            var mgr = new UpdateManager(
                new GithubSource("https://github.com/redyd/Looma", null, false),
                new UpdateOptions { ExplicitChannel = channel }
            );

            if (!mgr.IsInstalled) return;

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion is null) return;

            await mgr.DownloadUpdatesAsync(newVersion);
            mgr.ApplyUpdatesAndRestart(newVersion);
        }
        catch
        {
        }
    }

    private void ApplyStoredTheme()
    {
        var themeStorage = Services.GetRequiredService<IThemeStorage>();
        var selectedThemePath = themeStorage.GetSelectedThemePath();
        if (selectedThemePath is null)
            return;

        try
        {
            Services.GetRequiredService<ThemeService>().ApplyOverride(selectedThemePath);
        }
        catch
        {
            themeStorage.SaveSelectedTheme(null);
        }
    }

    private static int? GetSeedCount(IEnumerable<string> args)
    {
        const string seedPrefix = "--seed-";
        var seedArgument = args.FirstOrDefault(arg => arg.StartsWith(seedPrefix, StringComparison.Ordinal));
        if (seedArgument is null)
            return null;

        var value = seedArgument[seedPrefix.Length..];
        if (int.TryParse(value, out var count) && count >= 0)
            return count;

        throw new ArgumentException($"Argument de seed invalide: {seedArgument}. Utilisez --seed-N avec N >= 0.");
    }
}
