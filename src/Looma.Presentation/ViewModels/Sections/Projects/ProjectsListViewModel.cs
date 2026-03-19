using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectsListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;

    [ObservableProperty]
    private int _itemId;

    public ProjectsListViewModel(INavigationService nav)
    {
        _nav = nav;
        Title = "Projets";
    }
}