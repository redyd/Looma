using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Search;

namespace Looma.Presentation.ViewModels.Base;

public abstract partial class PaginatePageViewModelBase<TEntity, TViewModel, TKey>(int pageSize = 12, ISearchSpec<TEntity> searcher) : PageViewModelBase
{
    private IReadOnlyList<TEntity> _allEntities = [];
    private IReadOnlyList<TViewModel> _allSummaries = [];
    private IReadOnlyList<TViewModel> _filteredSummaries = [];

    [ObservableProperty]
    public partial ObservableCollection<TViewModel> CurrentPageEntities { get; set; } = [];

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial bool HasPreviousPage { get; set; }

    [ObservableProperty]
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
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplySearchAndPaging();
    }

    partial void OnCurrentPageChanged(int value) => ApplySearchAndPaging();
}
