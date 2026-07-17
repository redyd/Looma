// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System;
using System.Collections.Generic;
using Looma.Domain.Core;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;
using Looma.App.Services;
using Looma.Domain.Refresh;
using Looma.Domain.Search;
using Looma.Domain.Services;
using Looma.Infrastructure.Repositories;
using Looma.Infrastructure.Services;
using Looma.Infrastructure.Storage;
using Looma.Presentation.Notifications;
using Looma.Presentation.Navigation;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Main;
using Looma.Presentation.ViewModels.Sections.Stocks;
using Looma.Presentation.ViewModels.Sections.Projects;
using Looma.Presentation.ViewModels.Sections.Documents;
using Looma.Presentation.ViewModels.Sections.Patterns;
using Looma.Presentation.ViewModels.Sections.Statistics;
using Looma.Presentation.ViewModels.Sections.Settings;
using Microsoft.Extensions.DependencyInjection;
using Looma.Presentation.ViewModels.Shared.Documents;
using Looma.Domain.IServices;

namespace Looma.App;

public static class DependencyInjection
{
    public static void AddDomain(this IServiceCollection services, IReadOnlyCollection<string>? args = null)
    {
        services.AddSingleton<IDomainLogger, ConsoleDomainLogger>();
        services.AddSingleton<IDataRefreshService, DataRefreshService>();
        services.AddScoped<IAppDataSeeder, AppDataSeeder>();
#if DEBUG
        services.AddSingleton<MockUpdater>();
        services.AddSingleton<IUpdaterService>(sp => sp.GetRequiredService<MockUpdater>());
#else
        services.AddSingleton<IUpdaterService, GithubUpdater>();
#endif

        services.AddScoped<WoolStockCalculator>();
        services.AddScoped<IWoolService, WoolService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IPatternService, PatternService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IWoolStockService, WoolStockService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        services.AddScoped<DocumentSearchSpec>();
        services.AddScoped<ProjectSearchSpec>();
        services.AddScoped<PatternSearchSpec>();
        services.AddScoped<WoolSearchSpec>();
    }

    public static void AddPresentation(this IServiceCollection services)
    {
        // Un NavigationService PAR section (scope isolé)
        services.AddTransient<INavigationService, NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IUpdateInteractionService, UpdateInteractionService>();

        // PROJECTS
        services.AddTransient<ProjectsListViewModel>();
        services.AddTransient<ProjectsFormViewModel>();
        services.AddTransient<ProjectsDetailViewModel>();
        services.AddTransient<ProjectsFinishViewModel>();

        // STOCKS
        services.AddTransient<WoolListViewModel>();
        services.AddTransient<WoolDetailViewModel>();
        services.AddTransient<WoolFormViewModel>();

        // PATTERNS
        services.AddTransient<PatternsFormViewModel>();
        services.AddTransient<PatternsDetailViewModel>();
        services.AddTransient<PatternsListViewModel>();

        // DOCUMENTS
        services.AddTransient<DocumentsFormViewModel>();
        services.AddTransient<DocumentsListViewModel>();

        // STATISTICS
        services.AddTransient<StatisticsViewModel>();

        // SETTINGS
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddSingleton(_ => TranslationService.Current);

        services.AddTransient<DocumentsPickerFormViewModel>();

        services.AddSingleton<MainViewModel>(sp =>
        {
            SectionNavigationViewModel MakeSection<TList>(
                Func<INavigationService, TList> factory)
                where TList : PageViewModelBase
            {
                var nav = sp.GetRequiredService<INavigationService>();
                var initialVm = factory(nav);
                return new SectionNavigationViewModel(nav, initialVm);
            }

            return new MainViewModel(
                MakeSection<ProjectsListViewModel>(nav =>
                    new ProjectsListViewModel(nav,
                        sp.GetRequiredService<IProjectService>(),
                        sp.GetRequiredService<INotificationService>(),
                        sp.GetRequiredService<IDataRefreshService>(),
                        sp.GetRequiredService<TranslationService>())),
                
                MakeSection<WoolListViewModel>(nav =>
                    new WoolListViewModel(nav, sp.GetRequiredService<IWoolService>(),
                        sp.GetRequiredService<INotificationService>(),
                        sp.GetRequiredService<WoolSearchSpec>(),
                        sp.GetRequiredService<IDataRefreshService>())),
                
                MakeSection<PatternsListViewModel>(nav =>
                    new PatternsListViewModel(nav,
                        sp.GetRequiredService<IPatternService>(),
                        sp.GetRequiredService<INotificationService>(),
                        sp.GetRequiredService<IDataRefreshService>())),
                    
                MakeSection<DocumentsListViewModel>(nav =>
                    new DocumentsListViewModel(nav,
                        sp.GetRequiredService<IDocumentService>(),
                        sp.GetRequiredService<IPatternService>(),
                        sp.GetRequiredService<IProjectService>(),
                        sp.GetRequiredService<INotificationService>(),
                        sp.GetRequiredService<IDataRefreshService>())),

                MakeSection<StatisticsViewModel>(_ =>
                    new StatisticsViewModel(
                        sp.GetRequiredService<IStatisticsService>(),
                        sp.GetRequiredService<INotificationService>(),
                        sp.GetRequiredService<IDataRefreshService>())),

                MakeSection<SettingsViewModel>(nav =>
                    new SettingsViewModel(
                        nav,
                        sp.GetRequiredService<ThemeService>(),
                        sp.GetRequiredService<IThemeStorage>(),
                        sp.GetRequiredService<IThemeFilePicker>(),
                        sp.GetRequiredService<ISettingsService>(),
                        sp.GetRequiredService<INotificationService>(),
                        sp.GetRequiredService<TranslationService>(),
                        new SettingsUpdaterViewModel(
                            sp.GetRequiredService<INotificationService>(),
                            sp.GetRequiredService<IUpdaterService>(),
                            sp.GetRequiredService<IUpdateInteractionService>()),
                        sp.GetRequiredService<IDomainLogger>())),
                
                sp.GetRequiredService<INotificationService>(),
                sp.GetRequiredService<IUpdaterService>(),
                sp.GetRequiredService<IUpdateInteractionService>()
            );
        });
    }

    public static void AddInfrastructure(this IServiceCollection services)
    {
#if DEBUG
        // services.AddSingleton(new ApiSettings("http://localhost:8080"));
        services.AddSingleton(new ApiSettings("https://looma-api.redyd.dev"));
#else
        services.AddSingleton(new ApiSettings("https://looma-api.redyd.dev"));
#endif
        services.AddHttpClient<IReportService, ReportService>();

        services.AddSingleton<IDocumentFilePicker, AvaloniaDocumentFilePicker>();
        services.AddSingleton<IThemeFilePicker, AvaloniaThemeFilePicker>();
        services.AddSingleton<IThemeStorage, ThemeStorage>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IPatternRepository, PatternRepository>();
        services.AddScoped<IWoolRepository, WoolRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IWoolUsageRepository, WoolUsageRepository>();
        services.AddScoped<ITrackedWoolRepository, TrackedWoolRepository>();
    }
}
