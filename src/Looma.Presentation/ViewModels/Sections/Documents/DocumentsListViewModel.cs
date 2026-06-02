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

    [ObservableProperty] private ObservableCollection<DocumentSummaryViewModel> _currentDocuments = [];
    [ObservableProperty] private string _searchQuery = string.Empty;

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

    public override async void OnNavigatedTo() => await LoadAsync();

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
                _notifications.Error(result.Error ?? "Impossible de charger les documents.");
                ApplySearch();
                return;
            }

            _allDocuments = result.Value;
            _allSummaries = _allDocuments
                .Select(BuildSummary)
                .ToList();

            ApplySearch();
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

    partial void OnSearchQueryChanged(string value) => ApplySearch();

    private void ApplySearch()
    {
        var filteredIds = DocumentSearchSpec.Apply(_allDocuments, SearchQuery)
            .Select(document => document.Id)
            .ToHashSet();
        CurrentDocuments = new ObservableCollection<DocumentSummaryViewModel>(
            _allSummaries.Where(summary => filteredIds.Contains(summary.Document.Id)));
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

    [RelayCommand]
    private void OpenAddForm() =>
        _nav.NavigateTo<DocumentsFormViewModel>(vm => vm.InitCreate());
}
