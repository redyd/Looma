// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Main;

public partial class SectionNavigationViewModel : ViewModelBase
{
    private readonly INavigationService _nav;

    public INavigationService Navigation => _nav;

    [ObservableProperty] private PageViewModelBase? _currentPage;

    public bool CanGoBack => _nav.CanGoBack;

    public SectionNavigationViewModel(INavigationService nav, PageViewModelBase initialPage)
    {
        _nav = nav;
        _nav.Navigated += OnNavigated;

        // Charge la liste au démarrage
        _nav.PushPage(initialPage);
    }

    private void OnNavigated(object? sender, PageViewModelBase page)
    {
        CurrentPage = page;
        OnPropertyChanged(nameof(CanGoBack));
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack() => _nav.GoBack();
}