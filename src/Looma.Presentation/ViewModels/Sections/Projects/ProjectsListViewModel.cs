// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Domain.Refresh;
using Looma.Domain.Search;
using Looma.Domain.Services;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Shared;
using Looma.Presentation.ViewModels.Shared.Projects;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectsListViewModel(
    INavigationService nav,
    IProjectService projectService,
    INotificationService notifications,
    IDataRefreshService refreshService) : PaginatePageViewModelBase<Project, ProjectSummaryViewModel, int>(new ProjectSearchSpec())
{
    private bool _isInitialized;

    [ObservableProperty]
    public partial ProjectStatusFilterViewModel? SelectedStatusFilter { get; set; }
    
    public IReadOnlyList<ProjectStatusFilterViewModel> StatusFilters { get; } =
    [
        new("Tous les status", null),
        ..Enum.GetValues<Status>()
            .Select(type => new ProjectStatusFilterViewModel(type.GetDisplayName(), type))
    ];

    public override async void OnNavigatedTo()
    {
        RegisterRefresh(refreshService, RefreshScope.Projects, LoadAsync);

        if (!_isInitialized)
        {
            SelectedStatusFilter = StatusFilters.FirstOrDefault();
            _isInitialized = true;
        }

        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        GetEntityKey = project => project.ProjectId;
        GetSummaryKey = summary => summary.Project.ProjectId;
        
        Title = "Projets";
        IsBusy = true;

        try
        {
            var result = await projectService.GetAllAsync();
            if (result.Failed || result.Value is null)
            {
                notifications.Error(result.Error ?? "Impossible de charger les projets.");
                ClearPagesState();
                return;
            }

            var allProjects = result.Value;
            var allSummaries = allProjects.Select(BuildSummary).ToList();

            var filtered = SelectedStatusFilter?.Type is { } status
                ? allSummaries.Where(s => s.Project.Status == status).ToList()
                : allSummaries;

            ReloadPagesData(allProjects, [.. filtered]);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    partial void OnSelectedStatusFilterChanged(ProjectStatusFilterViewModel? value)
    {
        if (!_isInitialized)
            return;

        CurrentPage = 1;
        _ = LoadAsync();
    }

    private ProjectSummaryViewModel BuildSummary(Project project) =>
        new(project, new RelayCommand(() => nav.NavigateTo<ProjectsDetailViewModel>(vm => vm.Load(project))));


    [RelayCommand]
    private void OpenAddForm() => nav.NavigateTo<ProjectsFormViewModel>(vm => vm.InitCreate());
}
