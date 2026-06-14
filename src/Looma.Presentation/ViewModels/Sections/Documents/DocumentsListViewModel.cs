// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

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
using Looma.Presentation.ViewModels.Sections.Patterns;
using Looma.Presentation.ViewModels.Sections.Projects;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public partial class DocumentsListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IDocumentRepository _repo;
    private readonly IPatternRepository _patternRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly INotificationService _notifications;
    private readonly IDataRefreshService _refresh;

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
        IPatternRepository patternRepo,
        IProjectRepository projectRepo,
        INotificationService notifications,
        IDataRefreshService refresh)
    {
        _nav = nav;
        _repo = repo;
        _patternRepo = patternRepo;
        _projectRepo = projectRepo;
        _notifications = notifications;
        _refresh = refresh;
        Title = "Mes documents";
        _refresh.DocumentsRefreshRequested += OnDocumentsRefreshRequested;
    }

    public override async void OnNavigatedTo() => await RefreshAsync();

    public Task RefreshAsync() => LoadAsync();

    private void OnDocumentsRefreshRequested(object? sender, EventArgs e) => _ = RefreshAsync();

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
            new AsyncRelayCommand(() => OpenOriginAsync(document)),
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

    private async Task OpenOriginAsync(Document document)
    {
        if (document.PatternId.HasValue)
        {
            var result = await _patternRepo.GetByIdAsync(document.PatternId.Value);
            if (result.Failed || result.Value is null)
            {
                _notifications.Error(result.Error ?? "Impossible d'ouvrir le patron lié.");
                return;
            }

            _nav.NavigateTo<PatternsDetailViewModel>(vm => vm.Load(result.Value));
            return;
        }

        if (document.ProjectId.HasValue)
        {
            var result = await _projectRepo.GetByIdAsync(document.ProjectId.Value);
            if (result.Failed || result.Value is null)
            {
                _notifications.Error(result.Error ?? "Impossible d'ouvrir le projet lié.");
                return;
            }

            _nav.NavigateTo<ProjectsDetailViewModel>(vm => vm.Load(result.Value));
        }
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
        _refresh.RequestPatternsRefresh();
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
}
