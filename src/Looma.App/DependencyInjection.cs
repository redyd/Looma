using System;
using Looma.Domain.Repositories;
using Looma.App.Services;
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

namespace Looma.App;

public static class DependencyInjection
{
    public static void AddPresentation(this IServiceCollection services)
    {
        // Un NavigationService PAR section (scope isolé)
        services.AddTransient<INavigationService, NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // ViewModels — Transient pour être réinstanciés à chaque navigation

        // PROJECTS
        services.AddTransient<ProjectsListViewModel>();

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
                    new ProjectsListViewModel(nav)),
                MakeSection<WoolListViewModel>(nav =>
                    new WoolListViewModel(nav, sp.GetRequiredService<IWoolRepository>(),
                        sp.GetRequiredService<IStockRepository>(),
                        sp.GetRequiredService<INotificationService>())),
                MakeSection<PatternsListViewModel>(nav =>
                    new PatternsListViewModel(nav,
                        sp.GetRequiredService<IPatternRepository>(),
                        sp.GetRequiredService<INotificationService>())),
                MakeSection<DocumentsListViewModel>(nav =>
                    new DocumentsListViewModel(nav,
                        sp.GetRequiredService<IDocumentRepository>(),
                        sp.GetRequiredService<INotificationService>())),
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
        services.AddScoped<IStockRepository, StockRepository>();
    }
}
