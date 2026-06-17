// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;
using Looma.App.Services;
using Looma.Domain.Refresh;
using Looma.Domain.Search;
using Looma.Domain.Services;
using Looma.Infrastructure.Repositories;
using Looma.Presentation.Notifications;
using Looma.Presentation.Navigation;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Main;
using Looma.Presentation.ViewModels.Sections.Stocks;
using Looma.Presentation.ViewModels.Sections.Projects;
using Looma.Presentation.ViewModels.Sections.Documents;
using Looma.Presentation.ViewModels.Sections.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Looma.Presentation.ViewModels.Shared;
using Looma.Presentation.ViewModels.Shared.Documents;

namespace Looma.App;

public static class DependencyInjection
{
    public static void AddDomain(this IServiceCollection services)
    {
        services.AddScoped<WoolStockCalculator>();
        services.AddSingleton<IDomainLogger, ConsoleDomainLogger>();
        services.AddSingleton<IDataRefreshService, DataRefreshService>();
        services.AddScoped<IAppDataSeeder, AppDataSeeder>();
        services.AddScoped<IWoolService, WoolService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IPatternService, PatternService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IWoolStockService, WoolStockService>();

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
                        sp.GetRequiredService<IDataRefreshService>())),
                
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
                
                sp.GetRequiredService<INotificationService>()
            );
        });
    }

    public static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentFilePicker, AvaloniaDocumentFilePicker>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IPatternRepository, PatternRepository>();
        services.AddScoped<IWoolRepository, WoolRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IWoolUsageRepository, WoolUsageRepository>();
    }
}
