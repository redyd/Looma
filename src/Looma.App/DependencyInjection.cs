using System;
using Looma.Domain.Repositories;
using Looma.Infrastructure.Repositories;
using Looma.Presentation.Navigation;
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
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // Un NavigationService PAR section (scope isolé)
        services.AddTransient<INavigationService, NavigationService>();

        // ViewModels — Transient pour être réinstanciés à chaque navigation

        // PROJECTS
        services.AddTransient<ProjectsListViewModel>();

        // STOCKS
        services.AddTransient<WoolListViewModel>();
        services.AddTransient<WoolDetailViewModel>();
        services.AddTransient<WoolFormViewModel>();

        // PATTERNS
        services.AddTransient<PatternsListViewModel>();

        // DOCUMENTS
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
                        sp.GetRequiredService<IStockRepository>())),
                MakeSection<PatternsListViewModel>(nav =>
                    new PatternsListViewModel(nav)),
                MakeSection<DocumentsListViewModel>(nav =>
                    new DocumentsListViewModel(nav))
            );
        });

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IWoolRepository, WoolRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        return services;
    }
}