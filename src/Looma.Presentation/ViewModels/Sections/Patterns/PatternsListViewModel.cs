using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternsListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;

    [ObservableProperty]
    private int _itemId;

    public PatternsListViewModel(INavigationService nav)
    {
        _nav = nav;
        Title = "Patrons";
    }
}