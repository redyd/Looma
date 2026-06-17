// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Looma.App.Services;
using Looma.Infrastructure;
using Looma.Infrastructure.Storage;
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

        if (args.Contains("--seed"))
        {
            scope.ServiceProvider.GetRequiredService<IAppDataSeeder>()
                .SeedAsync()
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
}
