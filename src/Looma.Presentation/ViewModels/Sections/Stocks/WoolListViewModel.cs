// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Presentation.Notifications;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public record WoolSummary(Wool Wool, ICommand OpenDetailCommand);

public partial class WoolListViewModel(
    INavigationService nav,
    IWoolRepository woolRepo,
    INotificationService notifications)
    : PageViewModelBase
{
    private IReadOnlyList<Wool> _allWools = [];
    private IReadOnlyList<WoolSummary> _allSummaries = [];
    private IReadOnlyList<WoolSummary> _filteredSummaries = [];

    private const int PageSize = 12;

    [ObservableProperty] private ObservableCollection<WoolSummary> _currentPageWools = [];
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _hasPreviousPage;
    [ObservableProperty] private bool _hasNextPage;
    [ObservableProperty] private string _pageInfo = string.Empty;

    public override async void OnNavigatedTo() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var woolsResult = await woolRepo.GetAllAsync();
            if (woolsResult.Failed || woolsResult.Value is null)
            {
                _allWools = [];
                _allSummaries = [];
                notifications.Error(woolsResult.Error ?? "Impossible de charger les laines.");
                ApplySearchAndPaging();
                return;
            }

            _allWools = woolsResult.Value;

            var summaries = new List<WoolSummary>();
            foreach (var wool in _allWools)
            {
                summaries.Add(new WoolSummary(
                    wool,
                    new RelayCommand(() => nav.NavigateTo<WoolDetailViewModel>(vm => vm.Load(wool)))));
            }

            _allSummaries = summaries;

            ApplySearchAndPaging();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplySearchAndPaging();
    }

    partial void OnCurrentPageChanged(int value) => ApplySearchAndPaging();

    private void ApplySearchAndPaging()
    {
        var filteredWools = WoolSearchSpec.Apply(_allWools, SearchQuery).ToHashSet();
        _filteredSummaries = _allSummaries
            .Where(s => filteredWools.Contains(s.Wool))
            .ToList();

        TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredSummaries.Count / (double)PageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

        var page = _filteredSummaries
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        CurrentPageWools = new ObservableCollection<WoolSummary>(page);

        HasPreviousPage = CurrentPage > 1;
        HasNextPage = CurrentPage < TotalPages;
        PageInfo = $"{CurrentPage} / {TotalPages}";
    }

    [RelayCommand(CanExecute = nameof(HasPreviousPage))]
    private void PreviousPage()
    {
        CurrentPage--;
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasNextPage))]
    private void NextPage()
    {
        CurrentPage++;
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void OpenAddForm() =>
        nav.NavigateTo<WoolFormViewModel>(vm => vm.InitCreate());
}