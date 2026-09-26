// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System;
using System.Collections.Generic;
using System.Globalization;
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
using Looma.Domain.Core;
using Looma.Domain.IServices;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Main;
using Looma.Views.Views.Main;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Threading;
using Looma.Domain.Localization;
using Looma.Domain.Logging;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Recovery;
using Looma.Views.Views.Recovery;

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

        var args = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Args ?? [];

        services.AddPresentation();
        services.AddInfrastructure();
        services.AddDomain(args);
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

        // Domain and infrastructure messages use the same dictionary as the screens.
        Localizer.Current = Services.GetRequiredService<TranslationService>();
        ApplyStoredLanguage();

        // A staged import replaces the data files: it must happen before anything reads them.
        var dataGuard = Services.GetRequiredService<StartupDataGuard>();
        dataGuard.ApplyPendingReset();
        dataGuard.ApplyPendingRestore();

        SeedInternalThemes();
        // The restored preferences may carry another language.
        ApplyStoredLanguage();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LoomaDbContext>();

        if (args.Contains("--clear"))
        {
            db.Database.EnsureDeleted();
            pathManager.ClearDocuments();
        }

        var databaseResult = dataGuard.PrepareDatabase(db);
        if (databaseResult.Failed)
        {
            ShowRecoveryWindow(databaseResult.Error ?? string.Empty);
            base.OnFrameworkInitializationCompleted();
            return;
        }

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
        _ = HandleStartupUpdatesAsync();
        _ = ReportDataHealthAsync(dataGuard);
    }

    private void ShowRecoveryWindow(string problem)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        ApplyStoredTheme();
        desktop.MainWindow = new RecoveryWindow
        {
            DataContext = new RecoveryViewModel(
                problem,
                Services.GetRequiredService<IBackupService>(),
                Services.GetRequiredService<IBackupFilePicker>(),
                Services.GetRequiredService<IDatabaseRecovery>(),
                Services.GetRequiredService<IAppLifetimeService>())
        };
    }

    private static async Task ReportDataHealthAsync(StartupDataGuard dataGuard)
    {
        var notifications = Services.GetRequiredService<INotificationService>();
        var translation = Services.GetRequiredService<TranslationService>();

        if (dataGuard.PendingNotice is { } notice)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (notice.Success)
                    notifications.Success(notice.Message);
                else
                    notifications.Error(notice.Message);
            });
        }

        try
        {
            var report = await Services.GetRequiredService<IDataIntegrityService>().CheckAsync();
            if (report.Succeeded && report.Value is { IsHealthy: false } value)
            {
                Dispatcher.UIThread.Post(() => notifications.Warning(
                    translation.Format("Integrity_Notifications_IssuesFound", value.Issues.Count),
                    duration: TimeSpan.FromSeconds(12)));
            }
        }
        catch (Exception ex)
        {
            Services.GetRequiredService<IDomainLogger>().Log(DomainLogLevel.Warning, "Startup integrity check failed.", ex);
        }
    }

    private async Task HandleStartupUpdatesAsync()
    {
        var updater = Services.GetRequiredService<IUpdaterService>();
        var updateInteraction = Services.GetRequiredService<IUpdateInteractionService>();

        if (await updater.ShouldShowCurrentReleaseNotesAsync())
        {
            Dispatcher.UIThread.Post(updateInteraction.RequestCurrentReleaseNotes);
        }

        await updater.CheckForUpdatesAsync(silent: true);
        if (updater.Status == UpdateStatus.Available)
        {
            Dispatcher.UIThread.Post(updateInteraction.RequestUpdatePrompt);
        }
    }

    private void ApplyStoredTheme()
    {
        try
        {
            var themeStorage = Services.GetRequiredService<IThemeStorage>();
            var selectedThemePath = themeStorage.GetSelectedThemePath();
            if (selectedThemePath is null)
                return;

            Services.GetRequiredService<ThemeService>().ApplyOverride(selectedThemePath);
        }
        catch
        {
            // The settings screen surfaces the detailed configuration or theme parsing error.
        }
    }

    private static readonly CultureInfo SystemUiCulture = CultureInfo.CurrentUICulture;

    private void ApplyStoredLanguage()
    {
        var translation = Services.GetRequiredService<TranslationService>();
        var settings = Services.GetRequiredService<ISettingsService>();
        var result = settings.GetSelectedLanguageAsync().GetAwaiter().GetResult();
        var culture = result.Succeeded
            ? GetSupportedCulture(result.Value)
            : null;

        // Use the system language (not the current one, which a previous call may have changed).
        culture ??= GetSupportedCulture(SystemUiCulture.Name)
                    ?? GetSupportedCulture(SystemUiCulture.TwoLetterISOLanguageName);

        if (culture is not null)
            translation.SetCulture(culture);
    }

    private static string? GetSupportedCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        try
        {
            var cultureInfo = new CultureInfo(culture);
            return TranslationService.SupportedLanguage.FirstOrDefault(supported =>
                string.Equals(supported, culture, StringComparison.OrdinalIgnoreCase)
                || string.Equals(supported, cultureInfo.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static void SeedInternalThemes()
    {
        var sourceFolder = Path.Combine(AppContext.BaseDirectory, "Seed", "Themes");
        Services.GetRequiredService<IThemeStorage>().SeedThemeFiles(sourceFolder);
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
