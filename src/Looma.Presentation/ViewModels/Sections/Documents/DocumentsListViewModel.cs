// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Sections.Patterns;
using Looma.Presentation.ViewModels.Sections.Projects;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public partial class DocumentsListViewModel : PaginatePageViewModelBase<Document, DocumentSummaryViewModel, Guid>
{
    private readonly INavigationService _nav;
    private readonly IDocumentRepository _repo;
    private readonly IPatternRepository _patternRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly INotificationService _notifications;

    public DocumentsListViewModel(
        INavigationService nav,
        IDocumentRepository repo,
        IPatternRepository patternRepo,
        IProjectRepository projectRepo,
        INotificationService notifications) : base(searcher: new DocumentSearchSpec())
    {
        _nav = nav;
        _repo = repo;
        _patternRepo = patternRepo;
        _projectRepo = projectRepo;
        _notifications = notifications;
        Title = "Mes documents";

        GetEntityKey = document => document.Id;
        GetSummaryKey = summary => summary.Document.Id;
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
                _notifications.Error(result.Error ?? "Impossible de charger les documents.");
                ClearPagesState();
                return;
            }

            var all = result.Value;
            var summaries = all
                .Select(BuildSummary)
                .ToList();

            ReloadPagesData(all, summaries);
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
        await LoadAsync();
    }
}
