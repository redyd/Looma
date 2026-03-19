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
        services.AddTransient<StocksListViewModel>();

        // PATTERNS
        services.AddTransient<PatternsListViewModel>();

        // DOCUMENTS
        services.AddTransient<DocumentsListViewModel>();

        services.AddSingleton<MainViewModel>(sp =>
        {
            SectionNavigationViewModel MakeSection<TList>() where TList : PageViewModelBase
            {
                var nav = sp.GetRequiredService<INavigationService>();
                var initialVm = sp.GetRequiredService<TList>();
                return new SectionNavigationViewModel(nav, initialVm);
            }

            return new MainViewModel(
                MakeSection<ProjectsListViewModel>(),
                MakeSection<StocksListViewModel>(),
                MakeSection<PatternsListViewModel>(),
                MakeSection<DocumentsListViewModel>()
            );
        });

        return services;
    }
}