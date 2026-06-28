// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Domain.IServices;
using Looma.Domain.Refresh;
using Looma.Domain.Request;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.UserControls;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Sections.Projects;
using Looma.Presentation.ViewModels.Shared;
using Looma.Presentation.ViewModels.Shared.Patterns;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternsDetailViewModel(
    INavigationService nav,
    IPatternService patternService,
    IProjectService projectService,
    IDocumentService documentService,
    INotificationService notifications,
    IDataRefreshService refreshService) : PageViewModelBase
{
    private int _patternId;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Url { get; set; }

    [ObservableProperty]
    public partial string? Note { get; set; }

    [ObservableProperty]
    public partial PatternType Type { get; set; }

    [ObservableProperty]
    public partial bool IsPersonal { get; set; }

    [ObservableProperty]
    public partial DateOnly? BeginDate { get; set; }

    [ObservableProperty]
    public partial DateOnly? EndDate { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DocumentSummaryViewModel> Documents { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<PatternProjectViewModel> Projects { get; set; } = [];

    public bool HasUrl => !string.IsNullOrWhiteSpace(Url);
    public bool HasDocuments => Documents.Count > 0;
    public bool HasProjects => Projects.Count > 0;
    public string NoteDisplay => string.IsNullOrWhiteSpace(Note) ? Translation["Common_NoNote"] : Note!;

    public IList<StatItem> DetailStats =>
    [
        new() { Label = Translation["PatternsDetail_LinkedProjectsCount"], Value = Projects.Count.ToString("N0"), Unit = "x", IsFirst = true },
    ];

    public IList<InfoItem> DetailInfos =>
    [
        new() { Label = Translation["Common_Name"], Value = Name },
        new() { Label = Translation["Common_Link"], Value = Url ?? Translation["Common_None"], IsLink = HasUrl },
        new() { Label = Translation["Common_Type"], Value = Type.GetDisplayName() },
        new() { Label = Translation["Common_Pattern"], Value = IsPersonal ? Translation["Common_Personal"] : Translation["Common_NotPersonal"] },
        new() { Label = Translation["Common_Begin"], Value = BeginDate.FormatWithDefault(Translation["Common_NoneFeminine"]) },
        new() { Label = Translation["Common_End"], Value = EndDate.FormatWithDefault(Translation["Common_NoneFeminine"]) },
        new() { Label = Translation["Navigation_Documents"], Value = Documents.Count.ToString("N0") },
    ];

    public void Load(Pattern pattern)
    {
        Title = Translation["PatternsDetail_Title"];
        _patternId = pattern.Id;

        ApplyPattern(pattern);
    }

    public override async void OnNavigatedTo()
    {
        RegisterRefresh(refreshService, RefreshScope.Patterns | RefreshScope.Documents | RefreshScope.Projects, RefreshAsync);
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_patternId == 0) return;

        var pattern = await patternService.GetByIdAsync(_patternId);
        if (pattern.Failed || pattern.Value is null)
        {
            ErrorMessage = pattern.Error ?? Translation.Format("Patterns_Errors_PatternNotFound", _patternId);
            return;
        }

        ApplyPattern(pattern.Value);
    }

    private void ApplyPattern(Pattern pattern)
    {
        ErrorMessage = null;
        Name = pattern.Name;
        Url = pattern.Url;
        Note = pattern.Note;
        Type = pattern.Type;
        IsPersonal = pattern.IsPersonal;
        BeginDate = pattern.BeginDate;
        EndDate = pattern.EndDate;

        Documents = new ObservableCollection<DocumentSummaryViewModel>(
            pattern.Documents.Select(d => new DocumentSummaryViewModel(
                d,
                new AsyncRelayCommand(() => OpenDocumentAsync(d.Id)))));

        Projects = new ObservableCollection<PatternProjectViewModel>(pattern.Projects.Select(p => new PatternProjectViewModel
        {
            Name = p.Name,
            StatusDisplay = Translation[$"Enum_{p.Status}"],
            OpenCommand = new AsyncRelayCommand(() => OpenProjectAsync(p.Id))
        }));

        OnPropertyChanged(nameof(HasUrl));
        OnPropertyChanged(nameof(HasDocuments));
        OnPropertyChanged(nameof(HasProjects));
        OnPropertyChanged(nameof(NoteDisplay));
        OnPropertyChanged(nameof(DetailInfos));
    }

    private async Task OpenDocumentAsync(Guid id)
    {
        var result = await documentService.OpenAsync(id);
        if (result.Failed)
            notifications.Error(result.Error ?? Translation["Documents_Notifications_UnableToOpenDocument"]);
    }

    private async Task OpenProjectAsync(int id)
    {
        var result = await projectService.GetByIdAsync(id);
        if (result.Failed || result.Value is null)
        {
            notifications.Error(result.Error ?? Translation["Projects_Notifications_UnableToOpenProject"]);
            return;
        }

        nav.NavigateTo<ProjectsDetailViewModel>(vm => vm.Load(result.Value));
    }

    [RelayCommand]
    private async Task SaveNoteAsync()
    {
        if (_patternId == 0)
            return;

        IsBusy = true;
        try
        {
            var result = await patternService.UpdateAsync(new UpdatePatternRequest(
                _patternId,
                Name,
                Url,
                Note,
                Type,
                IsPersonal,
                BeginDate,
                EndDate));

            if (result.Failed || result.Value is null)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? Translation["Common_Notifications_UnableToUpdateNote"]);
                return;
            }

            ApplyPattern(result.Value);
            notifications.Success(Translation["Common_Notifications_NoteUpdated"]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(Url)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            notifications.Error(Translation.Format("Common_Notifications_UnableToOpenLink", ex.Message));
        }
    }

    [RelayCommand]
    private void Edit() =>
        nav.NavigateTo<PatternsFormViewModel>(async void (vm) =>
            await vm.InitEdit(_patternId, Name, Url, Note, Type, IsPersonal, BeginDate, EndDate,
                Documents.Select(d => d.Document.Id).ToList()));

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsBusy = true;
        try
        {
            var result = await patternService.DeleteAsync(_patternId);
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? Translation["Patterns_Notifications_UnableToDeletePattern"]);
                return;
            }

            notifications.Success(Translation["Patterns_Notifications_PatternDeleted"]);
            nav.GoBack();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoBack() => nav.GoBack();
}
