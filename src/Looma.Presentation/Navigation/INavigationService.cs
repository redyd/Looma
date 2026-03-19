using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.Navigation;

public interface INavigationService
{
    PageViewModelBase? CurrentPage { get; }
    bool CanGoBack { get; }

    void NavigateTo<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : PageViewModelBase;
    void PushPage(PageViewModelBase page);

    void GoBack();
    void ClearHistory();

    event EventHandler<PageViewModelBase>? Navigated;
}