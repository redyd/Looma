using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public partial class DocumentsListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IDocumentRepository _repo;
    private readonly INotificationService _notifications;

    private IReadOnlyList<Document> _allDocuments = [];
    private IReadOnlyList<DocumentSummaryViewModel> _allSummaries = [];
    private IReadOnlyList<DocumentSummaryViewModel> _filteredSummaries = [];

    private const int PageSize = 12;

    [ObservableProperty] private ObservableCollection<DocumentSummaryViewModel> _currentPageDocuments = [];
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _hasPreviousPage;
    [ObservableProperty] private bool _hasNextPage;
    [ObservableProperty] private string _pageInfo = string.Empty;

    public DocumentsListViewModel(
        INavigationService nav,
        IDocumentRepository repo,
        INotificationService notifications)
    {
        _nav = nav;
        _repo = repo;
        _notifications = notifications;
        Title = "Mes documents";
    }

    public override async void OnNavigatedTo() => await RefreshAsync();

    public Task RefreshAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _repo.GetAllAsync();
            if (result.Failed || result.Value is null)
            {
                _allDocuments = [];
                _allSummaries = [];
                _filteredSummaries = [];
                _notifications.Error(result.Error ?? "Impossible de charger les documents.");
                ApplySearchAndPaging();
                return;
            }

            _allDocuments = result.Value;
            _allSummaries = _allDocuments
                .Select(BuildSummary)
                .ToList();

            ApplySearchAndPaging();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private DocumentSummaryViewModel BuildSummary(Document document) =>
        new(
            document,
            new AsyncRelayCommand(() => OpenAsync(document.Id)),
            new RelayCommand(() => _nav.NavigateTo<DocumentsFormViewModel>(vm => vm.InitEdit(document.Id, document.Nickname))),
            new AsyncRelayCommand(() => DeleteAsync(document.Id)));

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplySearchAndPaging();
    }

    partial void OnCurrentPageChanged(int value) => ApplySearchAndPaging();

    private void ApplySearchAndPaging()
    {
        var filteredIds = DocumentSearchSpec.Apply(_allDocuments, SearchQuery)
            .Select(document => document.Id)
            .ToHashSet();

        _filteredSummaries = _allSummaries
            .Where(summary => filteredIds.Contains(summary.Document.Id))
            .ToList();

        TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredSummaries.Count / (double)PageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

        var page = _filteredSummaries
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);

        CurrentPageDocuments = new ObservableCollection<DocumentSummaryViewModel>(page);

        HasPreviousPage = CurrentPage > 1;
        HasNextPage = CurrentPage < TotalPages;
        PageInfo = $"{CurrentPage} / {TotalPages}";
    }

    private async Task OpenAsync(Guid id)
    {
        var result = await _repo.OpenAsync(id);
        if (result.Failed)
            _notifications.Error(result.Error ?? "Impossible d'ouvrir le document.");
    }

    private async Task DeleteAsync(Guid id)
    {
        var result = await _repo.DeleteAsync(id);
        if (result.Failed)
        {
            _notifications.Error(result.Error ?? "Impossible de supprimer le document.");
            return;
        }

        _notifications.Success("Le document a été supprimé.");
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
        _nav.NavigateTo<DocumentsFormViewModel>(vm => vm.InitCreate());
}
