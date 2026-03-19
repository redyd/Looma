using Looma.Presentation.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Looma.Presentation.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    private readonly NavigationStack<PageViewModelBase> _stack = new();

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public PageViewModelBase? CurrentPage => _stack.Current;
    public bool CanGoBack => _stack.CanGoBack;

    public event EventHandler<PageViewModelBase>? Navigated;

    public void NavigateTo<TViewModel>(Action<TViewModel>? configure = null)
        where TViewModel : PageViewModelBase
    {
        var vm = _services.GetRequiredService<TViewModel>();
        configure?.Invoke(vm);
        _stack.Push(vm);
        vm.OnNavigatedTo();
        Navigated?.Invoke(this, vm);
    }
    
    public void PushPage(PageViewModelBase page)
    {
        _stack.Push(page);
        page.OnNavigatedTo();
        Navigated?.Invoke(this, page);
    }

    public void GoBack()
    {
        if (!CanGoBack) return;
        var vm = _stack.Pop();
        if (vm is not null)
        {
            vm.OnNavigatedTo();
            Navigated?.Invoke(this, vm);
        }
    }

    public void ClearHistory() => _stack.Clear();
}