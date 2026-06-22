// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Presentation.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Looma.Presentation.Navigation;

public class NavigationService(IServiceProvider services) : INavigationService
{
    private readonly IServiceProvider _services = services;
    private readonly NavigationStack<PageViewModelBase> _stack = new();

    public PageViewModelBase? CurrentPage => _stack.Current;
    public bool CanGoBack => _stack.CanGoBack;

    public event EventHandler<PageViewModelBase>? Navigated;

    public void NavigateTo<TViewModel>(Action<TViewModel>? configure = null)
        where TViewModel : PageViewModelBase
    {
        var vm = ActivatorUtilities.CreateInstance<TViewModel>(_services, this);
        configure?.Invoke(vm);
        CurrentPage?.OnNavigatedFrom();
        DestroyPages(_stack.Push(vm));
        vm.OnNavigatedTo();
        Navigated?.Invoke(this, vm);
    }

    public void PushPage(PageViewModelBase page)
    {
        CurrentPage?.OnNavigatedFrom();
        DestroyPages(_stack.Push(page));
        page.OnNavigatedTo();
        Navigated?.Invoke(this, page);
    }

    public void GoBack()
    {
        if (!CanGoBack) return;
        CurrentPage?.OnNavigatedFrom();
        var vm = _stack.Pop(out var removedCurrent);
        removedCurrent?.Destroy();
        if (vm is not null)
        {
            vm.OnNavigatedTo();
            Navigated?.Invoke(this, vm);
        }
    }

    public void ClearHistory() => DestroyPages(_stack.Clear());

    private static void DestroyPages(IEnumerable<PageViewModelBase> pages)
    {
        foreach (var page in pages)
        {
            page.Destroy();
        }
    }
}
