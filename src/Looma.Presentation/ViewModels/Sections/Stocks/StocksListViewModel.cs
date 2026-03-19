using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public partial class StocksListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;

    [ObservableProperty]
    private int _itemId;

    public StocksListViewModel(INavigationService nav)
    {
        _nav = nav;
        Title = "Stocks";
    }
}