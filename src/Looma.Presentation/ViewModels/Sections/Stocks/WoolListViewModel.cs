using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Stocks;

public partial class WoolListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IWoolRepository _repo;
    private IReadOnlyList<Wool> _allWools = [];
    private IReadOnlyList<Wool> _filteredWools = [];

    private const int PageSize = 12;

    [ObservableProperty] private ObservableCollection<Wool> _currentPageWools = [];
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _hasPreviousPage;
    [ObservableProperty] private bool _hasNextPage;
    [ObservableProperty] private string _pageInfo = string.Empty;

    public WoolListViewModel(INavigationService nav, IWoolRepository repo)
    {
        _nav = nav;
        _repo = repo;
    }

    public override async void OnNavigatedTo() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        _allWools = await _repo.GetAllAsync();
        ApplySearchAndPaging();
        IsBusy = false;
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplySearchAndPaging();
    }

    partial void OnCurrentPageChanged(int value) => ApplySearchAndPaging();

    private void ApplySearchAndPaging()
    {
        _filteredWools = WoolSearchSpec.Apply(_allWools, SearchQuery).ToList();

        TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredWools.Count / (double)PageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

        var page = _filteredWools
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        CurrentPageWools = new ObservableCollection<Wool>(page);

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

    [RelayCommand]
    private void OpenDetail(Wool wool) =>
        _nav.NavigateTo<WoolDetailViewModel>(vm => vm.Load(wool));
}