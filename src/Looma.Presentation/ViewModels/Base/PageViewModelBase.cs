// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Domain.Refresh;

namespace Looma.Presentation.ViewModels.Base;

public abstract partial class PageViewModelBase : ViewModelBase
{
    private readonly List<Action> _refreshUnsubscribers = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    /// <summary>Appelé à chaque fois que la page devient active.</summary>
    public virtual void OnNavigatedTo()
    {
    }

    /// <summary>Appelé quand on quitte la page.</summary>
    public virtual void OnNavigatedFrom()
    {
        foreach (var unsubscribe in _refreshUnsubscribers)
        {
            unsubscribe();
        }

        _refreshUnsubscribers.Clear();
    }

    protected void RegisterRefresh(
        IDataRefreshService refreshService,
        RefreshScope scope,
        Func<Task> refreshAsync)
    {
        if (_refreshUnsubscribers.Count > 0)
            return;

        EventHandler<DataRefreshRequestedEventArgs> handler = (_, args) =>
        {
            if ((args.Scope & scope) == RefreshScope.None)
                return;

            if (IsBusy)
                return;

            _ = refreshAsync();
        };

        refreshService.RefreshRequested += handler;
        _refreshUnsubscribers.Add(() => refreshService.RefreshRequested -= handler);
    }
}
