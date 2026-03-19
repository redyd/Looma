using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public partial class DocumentsListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;

    [ObservableProperty]
    private int _itemId;

    public DocumentsListViewModel(INavigationService nav)
    {
        _nav = nav;
        Title = "Patrons";
    }
}