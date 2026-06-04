using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternsListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IPatternRepository _repo;
    private readonly INotificationService _notifications;
    private readonly IDataRefreshService _refresh;

    private IReadOnlyList<Pattern> _allPatterns = [];
    private IReadOnlyList<PatternSummaryViewModel> _allSummaries = [];
    private IReadOnlyList<PatternSummaryViewModel> _filteredSummaries = [];

    private const int PageSize = 12;

    [ObservableProperty] private ObservableCollection<PatternSummaryViewModel> _currentPagePatterns = [];
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _hasPreviousPage;
    [ObservableProperty] private bool _hasNextPage;
    [ObservableProperty] private string _pageInfo = string.Empty;

    public PatternsListViewModel(
        INavigationService nav,
        IPatternRepository repo,
        INotificationService notifications,
        IDataRefreshService refresh)
    {
        _nav = nav;
        _repo = repo;
        _notifications = notifications;
        _refresh = refresh;
        Title = "Mes patrons";
        _refresh.PatternsRefreshRequested += OnPatternsRefreshRequested;
    }

    public override async void OnNavigatedTo() => await LoadAsync();

    public Task RefreshAsync() => LoadAsync();

    private void OnPatternsRefreshRequested(object? sender, EventArgs e) => _ = RefreshAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _repo.GetAllAsync();
            if (result.Failed || result.Value is null)
            {
                _allPatterns = [];
                _allSummaries = [];
                _filteredSummaries = [];
                _notifications.Error(result.Error ?? "Impossible de charger les patrons.");
                ApplySearchAndPaging();
                return;
            }

            _allPatterns = result.Value;
            _allSummaries = _allPatterns
                .Select(BuildSummary)
                .ToList();

            ApplySearchAndPaging();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private PatternSummaryViewModel BuildSummary(Pattern pattern) =>
        new(
            pattern,
            pattern.Documents.Count,
            pattern.Projects.Count,
            !string.IsNullOrWhiteSpace(pattern.Url),
            new RelayCommand(() => _nav.NavigateTo<PatternsDetailViewModel>(vm => vm.Load(pattern))));

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplySearchAndPaging();
    }

    partial void OnCurrentPageChanged(int value) => ApplySearchAndPaging();

    private void ApplySearchAndPaging()
    {
        var filteredPatterns = PatternSearchSpec.Apply(_allPatterns, SearchQuery).ToHashSet();
        _filteredSummaries = _allSummaries
            .Where(s => filteredPatterns.Contains(s.Pattern))
            .ToList();

        TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredSummaries.Count / (double)PageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

        var page = _filteredSummaries
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        CurrentPagePatterns = new ObservableCollection<PatternSummaryViewModel>(page);
        HasPreviousPage = CurrentPage > 1;
        HasNextPage = CurrentPage < TotalPages;
        PageInfo = $"{CurrentPage} / {TotalPages}";
    }

    private async Task DeleteAsync(int id)
    {
        var result = await _repo.DeleteAsync(id);
        if (result.Failed)
        {
            _notifications.Error(result.Error ?? "Impossible de supprimer le patron.");
            return;
        }

        _notifications.Success("Le patron a été supprimé.");
        await LoadAsync();
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
        _nav.NavigateTo<PatternsFormViewModel>(vm => vm.InitCreate());
}
