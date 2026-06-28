// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Entities;
using Looma.Domain.IServices;
using Looma.Domain.Refresh;
using Looma.Domain.Search;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Sections.Patterns;
using Looma.Presentation.ViewModels.Sections.Projects;
using Looma.Presentation.ViewModels.Shared;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public partial class DocumentsListViewModel(
    INavigationService nav,
    IDocumentService documentService,
    IPatternService patternService,
    IProjectService projectService,
    INotificationService notifications,
    IDataRefreshService refreshService)
: PaginatePageViewModelBase<Document, DocumentSummaryViewModel, Guid>(searcher: new DocumentSearchSpec())
{
    public override bool KeepAliveInNavigationHistory => true;

    public override async void OnNavigatedTo()
    {
        RegisterRefresh(refreshService, RefreshScope.Documents, LoadAsync);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Title = Translation["Documents_Title"];
        GetEntityKey = document => document.Id;
        GetSummaryKey = summary => summary.Document.Id;

        IsBusy = true;
        try
        {
            var result = await documentService.GetAllAsync();
            if (result.Failed || result.Value is null)
            {
                notifications.Error(result.Error ?? Translation["Documents_Notifications_UnableToLoadDocuments"]);
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
            new RelayCommand(() => nav.NavigateTo<DocumentsFormViewModel>(vm => vm.InitEdit(document.Id, document.Nickname))),
            new AsyncRelayCommand(() => DeleteAsync(document.Id)));

    private async Task OpenAsync(Guid id)
    {
        var result = await documentService.OpenAsync(id);
        if (result.Failed)
            notifications.Error(result.Error ?? Translation["Documents_Notifications_UnableToOpenDocument"]);
    }

    private async Task OpenOriginAsync(Document document)
    {
        if (document.PatternId.HasValue)
        {
            var result = await patternService.GetByIdAsync(document.PatternId.Value);
            if (result.Failed || result.Value is null)
            {
                notifications.Error(result.Error ?? Translation["Documents_Notifications_UnableToOpenLinkedPattern"]);
                return;
            }

            nav.NavigateTo<PatternsDetailViewModel>(vm => vm.Load(result.Value));
            return;
        }

        if (document.ProjectId.HasValue)
        {
            var result = await projectService.GetByIdAsync(document.ProjectId.Value);
            if (result.Failed || result.Value is null)
            {
                notifications.Error(result.Error ?? Translation["Documents_Notifications_UnableToOpenLinkedProject"]);
                return;
            }

            nav.NavigateTo<ProjectsDetailViewModel>(vm => vm.Load(result.Value));
        }
    }

    private async Task DeleteAsync(Guid id)
    {
        var result = await documentService.DeleteAsync(id);
        if (result.Failed)
        {
            notifications.Error(result.Error ?? Translation["Documents_Notifications_UnableToDeleteDocument"]);
            return;
        }

        notifications.Success(Translation["Documents_Notifications_DocumentDeleted"]);
    }
}
