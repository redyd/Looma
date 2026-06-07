// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

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