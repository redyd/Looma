// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Domain.Repositories;
using Looma.Domain.Search;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Shared;
using Looma.Presentation.ViewModels.Shared.Projects;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectsListViewModel(
    INavigationService nav,
    IProjectRepository projectRepo,
    INotificationService notifications) : PaginatePageViewModelBase<Project, ProjectSummaryViewModel, int>(new ProjectSearchSpec())
{

    [ObservableProperty]
    public partial ProjectStatusFilterViewModel? SelectedPatternTypeFilter { get; set; }
    
    public IReadOnlyList<ProjectStatusFilterViewModel> PatternTypeFilters { get; } =
    [
        new("Tous les status", null),
        ..Enum.GetValues<Status>()
            .Select(type => new ProjectStatusFilterViewModel(type.GetDisplayName(), type))
    ];

    public override async void OnNavigatedTo() => await LoadAsync();

    public async Task LoadAsync()
    {
        Title = "Projets";
        IsBusy = true;

        try
        {
            var result = await projectRepo.GetAllAsync();
            if (result.Failed || result.Value is null)
            {
                notifications.Error(result.Error ?? "Impossible de charger les projets.");
                ClearPagesState();
                return;
            }

            var allProjects = result.Value;
            var allSummaries = allProjects.Select(BuildSummary);

            if (SelectedPatternTypeFilter is not null)
            {
                allSummaries = allSummaries.Where(s => s.Project.Status == SelectedPatternTypeFilter.Type);
            }

            ReloadPagesData(allProjects, [.. allSummaries]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ProjectSummaryViewModel BuildSummary(Project project) =>
        new(project, new RelayCommand(() => nav.NavigateTo<ProjectsDetailViewModel>(vm => vm.Load(project))));


    [RelayCommand]
    private void OpenAddForm() => nav.NavigateTo<ProjectsFormViewModel>(vm => vm.InitCreate());
}
