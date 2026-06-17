// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Search;

namespace Looma.Presentation.ViewModels.Base;

public abstract partial class PaginatePageViewModelBase<TEntity, TViewModel, TKey>(ISearchSpec<TEntity> searcher, int pageSize = 12) : PageViewModelBase
{
    private IReadOnlyList<TEntity> _allEntities = [];
    private IReadOnlyList<TViewModel> _allSummaries = [];
    private IReadOnlyList<TViewModel> _filteredSummaries = [];

    public bool IsListEmpty => _filteredSummaries.Count == 0;
    public bool IsSourceListEmpty => _allEntities.Count == 0;
    public bool IsFilterResultEmpty => _allEntities.Count > 0 && _filteredSummaries.Count == 0;
    
    [ObservableProperty]
    public partial ObservableCollection<TViewModel> CurrentPageEntities { get; set; } = [];

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    public partial bool HasPreviousPage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial bool HasNextPage { get; set; }

    [ObservableProperty]
    public partial string PageInfo { get; set; } = string.Empty;

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

    protected Func<TEntity, TKey>? GetEntityKey { get; set; }
    protected Func<TViewModel, TKey>? GetSummaryKey { get; set; }

    protected void ClearPagesState()
    {
        _allEntities = [];
        _allSummaries = [];
        _filteredSummaries = [];

        ApplySearchAndPaging();
    }

    protected void ReloadPagesData(IReadOnlyList<TEntity> data, IReadOnlyList<TViewModel> summaries)
    {
        _allEntities = data;
        _allSummaries = summaries;

        ApplySearchAndPaging();
    }

    private void ApplySearchAndPaging()
    {
        if (GetEntityKey is null || GetSummaryKey is null)
        {
            throw new InvalidOperationException("GetEntityKey and GetSummaryKey must be set before applying search and paging.");
        }

        var filteredIds = searcher.Apply(_allEntities, SearchQuery)
            .Select(GetEntityKey)
            .ToHashSet();

        _filteredSummaries = [.. _allSummaries.Where(summary => filteredIds.Contains(GetSummaryKey!(summary)))];

        TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredSummaries.Count / (double)pageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

        var page = _filteredSummaries
            .Skip((CurrentPage - 1) * pageSize)
            .Take(pageSize);

        CurrentPageEntities = new ObservableCollection<TViewModel>(page);

        HasPreviousPage = CurrentPage > 1;
        HasNextPage = CurrentPage < TotalPages;
        PageInfo = $"{CurrentPage} / {TotalPages}";
        
        OnPropertyChanged(nameof(IsListEmpty));
        OnPropertyChanged(nameof(IsSourceListEmpty));
        OnPropertyChanged(nameof(IsFilterResultEmpty));
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplySearchAndPaging();
    }

    partial void OnCurrentPageChanged(int value) => ApplySearchAndPaging();
}
