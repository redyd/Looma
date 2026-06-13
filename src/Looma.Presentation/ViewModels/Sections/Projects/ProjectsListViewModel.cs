// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
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

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectsListViewModel : PageViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IProjectRepository _projectRepo;
    private readonly INotificationService _notifications;
    private IReadOnlyList<Project> _allProjects = [];
    private IReadOnlyList<ProjectSummaryViewModel> _allSummaries = [];
    private IReadOnlyList<ProjectSummaryViewModel> _filteredSummaries = [];

    private const int PageSize = 12;

    [ObservableProperty] private ObservableCollection<ProjectSummaryViewModel> _currentPageProjects = [];
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private ProjectStatusFilterViewModel _selectedStatusFilter;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _hasPreviousPage;
    [ObservableProperty] private bool _hasNextPage;
    [ObservableProperty] private string _pageInfo = string.Empty;

    public IReadOnlyList<ProjectStatusFilterViewModel> StatusFilters { get; } =
    [
        new("Tous les statuts", null),
        .. Enum.GetValues<Status>()
            .Select(status => new ProjectStatusFilterViewModel(status.GetDisplayName(), status))
    ];

    public ProjectsListViewModel(
        INavigationService nav,
        IProjectRepository projectRepo,
        INotificationService notifications)
    {
        _nav = nav;
        _projectRepo = projectRepo;
        _notifications = notifications;
        SelectedStatusFilter = StatusFilters[0];
        Title = "Projets";
    }

    public override async void OnNavigatedTo() => await LoadAsync();

    public async Task LoadAsync()
    {
        Title = "Projets";
        SelectedStatusFilter ??= StatusFilters[0];
        IsBusy = true;
        try
        {
            var result = await _projectRepo.GetAllAsync();
            if (result.Failed || result.Value is null)
            {
                _allProjects = [];
                _allSummaries = [];
                _filteredSummaries = [];
                _notifications.Error(result.Error ?? "Impossible de charger les projets.");
                ApplySearchAndPaging();
                return;
            }

            _allProjects = result.Value;
            _allSummaries = _allProjects.Select(BuildSummary).ToList();
            ApplySearchAndPaging();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ProjectSummaryViewModel BuildSummary(Project project) =>
        new(project, new RelayCommand(() => _nav.NavigateTo<ProjectsDetailViewModel>(vm => vm.Load(project))));

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplySearchAndPaging();
    }

    partial void OnSelectedStatusFilterChanged(ProjectStatusFilterViewModel value)
    {
        CurrentPage = 1;
        ApplySearchAndPaging();
    }

    partial void OnCurrentPageChanged(int value) => ApplySearchAndPaging();

    private void ApplySearchAndPaging()
    {
        var filteredProjects = ProjectSearchSpec
            .Apply(_allProjects, SearchQuery, SelectedStatusFilter?.Status)
            .ToHashSet();

        _filteredSummaries = _allSummaries
            .Where(summary => filteredProjects.Contains(summary.Project))
            .OrderBy(summary => summary.Project.BeginDate is null)
            .ThenBy(summary => summary.Project.BeginDate)
            .ThenBy(summary => summary.Project.Name)
            .ToList();

        TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredSummaries.Count / (double)PageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

        CurrentPageProjects = new ObservableCollection<ProjectSummaryViewModel>(
            _filteredSummaries
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize));

        HasPreviousPage = CurrentPage > 1;
        HasNextPage = CurrentPage < TotalPages;
        PageInfo = $"{CurrentPage} / {TotalPages}";
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasPreviousPage))]
    private void PreviousPage() => CurrentPage--;

    [RelayCommand(CanExecute = nameof(HasNextPage))]
    private void NextPage() => CurrentPage++;

    [RelayCommand]
    private void OpenAddForm() =>
        _nav.NavigateTo<ProjectsFormViewModel>(vm => vm.InitCreate());
}
