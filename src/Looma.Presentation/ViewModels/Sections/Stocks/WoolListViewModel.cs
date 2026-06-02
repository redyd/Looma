using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Presentation.Notifications;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public record WoolSummary(Wool Wool, double TotalWeightGrams, ICommand OpenDetailCommand);

public partial class WoolListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IWoolRepository _woolRepo;
    private readonly IStockRepository _stockRepo;
    private readonly INotificationService _notifications;

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

    public WoolListViewModel(
        INavigationService nav,
        IWoolRepository woolRepo,
        IStockRepository stockRepo,
        INotificationService notifications)
    {
        _nav = nav;
        _woolRepo = woolRepo;
        _stockRepo = stockRepo;
        _notifications = notifications;
    }

    public override async void OnNavigatedTo() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var woolsResult = await _woolRepo.GetAllAsync();
            if (woolsResult.Failed || woolsResult.Value is null)
            {
                _allWools = [];
                _allSummaries = [];
                _notifications.Error(woolsResult.Error ?? "Impossible de charger les laines.");
                ApplySearchAndPaging();
                return;
            }

            _allWools = woolsResult.Value;

            var summaries = new List<WoolSummary>();
            foreach (var wool in _allWools)
            {
                var totalResult = await _stockRepo.GetTotalWeightByWoolIdAsync(wool.Id);
                if (totalResult.Failed)
                    _notifications.Warning(totalResult.Error ?? $"Impossible de calculer le stock de {wool.Name}.");
                summaries.Add(new WoolSummary(
                    wool,
                    totalResult.Succeeded ? totalResult.Value : 0,
                    new RelayCommand(() => _nav.NavigateTo<WoolDetailViewModel>(vm => vm.Load(wool)))));
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
        _nav.NavigateTo<WoolFormViewModel>(vm => vm.InitCreate());

}
