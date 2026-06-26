// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.IServices;
using Looma.Domain.Refresh;
using Looma.Domain.Search;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Shared.Projects;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectsListViewModel : PaginatePageViewModelBase<Project, ProjectSummaryViewModel, int>
{
    private readonly INavigationService _nav;
    private readonly IProjectService _projectService;
    private readonly INotificationService _notifications;
    private readonly IDataRefreshService _refreshService;
    private readonly TranslationService _translation;
    private bool _isInitialized;

    public ProjectsListViewModel(
        INavigationService nav,
        IProjectService projectService,
        INotificationService notifications,
        IDataRefreshService refreshService,
        TranslationService translation) : base(new ProjectSearchSpec())
    {
        _nav = nav;
        _projectService = projectService;
        _notifications = notifications;
        _refreshService = refreshService;
        _translation = translation;

        StatusFilters = [];
        translation.PropertyChanged += (_, _) => RefreshStatusFilters();
    }

    public override bool KeepAliveInNavigationHistory => true;
    public TranslationService Translation => _translation;

    [ObservableProperty]
    public partial ProjectStatusFilterViewModel? SelectedStatusFilter { get; set; }
    public ObservableCollection<ProjectStatusFilterViewModel> StatusFilters { get; } = [];

    private void RefreshStatusFilters()
    {
        var previousType = SelectedStatusFilter?.Type;

        StatusFilters.Clear();
        StatusFilters.Add(new(_translation["Projects_AllStatusesFilter"], null));
        foreach (var status in Enum.GetValues<Status>())
            StatusFilters.Add(new(_translation[$"Enum_{status}"], status));

        SelectedStatusFilter = StatusFilters.FirstOrDefault(f => f.Type == previousType)
                               ?? StatusFilters.FirstOrDefault();
    }

    public override async void OnNavigatedTo()
    {
        RegisterRefresh(_refreshService, RefreshScope.Projects, LoadAsync);
        RefreshStatusFilters();

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

        Title = _translation["Projects_Title"];
        IsBusy = true;

        try
        {
            var result = await _projectService.GetAllAsync();
            if (result.Failed || result.Value is null)
            {
                _notifications.Error(result.Error ?? _translation["Projects_Notifications_UnableToLoadTheProjects"]);
                ClearPagesState();
                return;
            }

            var allProjects = result.Value;
            var allSummaries = allProjects.Select(BuildSummary).ToList();

            var filtered = SelectedStatusFilter?.Type is { } status
                ? [.. allSummaries.Where(s => s.Project.Status == status)]
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
        new(project, new RelayCommand(() => _nav.NavigateTo<ProjectsDetailViewModel>(vm => vm.Load(project))));


    [RelayCommand]
    private void OpenAddForm() => _nav.NavigateTo<ProjectsFormViewModel>(vm => vm.InitCreate());
}
