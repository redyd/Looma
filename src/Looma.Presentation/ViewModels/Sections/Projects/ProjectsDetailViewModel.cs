// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Domain.Refresh;
using Looma.Domain.Request;
using Looma.Domain.Services;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.UserControls;
using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Sections.Patterns;
using Looma.Presentation.ViewModels.Shared.Projects;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectsDetailViewModel(
    INavigationService nav,
    IProjectService projectService,
    INotificationService notifications,
    IWoolStockService stockService,
    IDocumentFilePicker documentFilePicker,
    IDataRefreshService refreshService)
    : PageViewModelBase
{
    private Project? _project;

    [ObservableProperty]
    public partial int ProjectId { get; set; }
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;
    [ObservableProperty]
    public partial Status Status { get; set; }
    [ObservableProperty]
    public partial string? Note { get; set; }
    [ObservableProperty]
    public partial DateOnly? BeginDate { get; set; }
    [ObservableProperty]
    public partial DateOnly? EndDate { get; set; }
    [ObservableProperty]
    public partial Pattern? Pattern { get; set; }
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<Shared.Projects.ProjectWoolUsageViewModel> Wools { get; set; } = [];
    [ObservableProperty]
    public partial ObservableCollection<ProjectImageViewModel> Images { get; set; } = [];
    [ObservableProperty]
    public partial int SelectedImageIndex { get; set; }
    [ObservableProperty]
    public partial double? WoolAdjustmentQuantity { get; set; }
    [ObservableProperty]
    public partial StockAdjustmentMode WoolAdjustmentMode { get; set; } = StockAdjustmentMode.ByBall;
    [ObservableProperty]
    public partial StockAdjustmentMode WoolDisplayMode { get; set; } = StockAdjustmentMode.ByBall;
    [ObservableProperty]
    public partial bool DeductWoolImmediately { get; set; }

    public string StatusDisplay => Status.GetDisplayName();
    public string NoteDisplay => string.IsNullOrWhiteSpace(Note) ? "Aucune note." : Note!;
    public string PatternName => Pattern?.Name ?? "Aucun patron";
    public string PatternTypeDisplay => Pattern?.Type.GetDisplayName() ?? "-";
    public string PatternNoteDisplay => string.IsNullOrWhiteSpace(Pattern?.Note) ? "Aucune note." : Pattern.Note!;
    public bool HasWools => Wools.Count > 0;
    public bool HasImages => Images.Count > 0;
    public bool HasMultipleImages => Images.Count > 1;
    public bool IsWishlist => Status == Status.Wishlist;
    public bool IsInProgress => Status == Status.InProgress;
    public bool IsPaused => Status == Status.Paused;
    public bool HasProjectActions => Status != Status.Finished;
    public bool CanAdjustWool => WoolAdjustmentQuantity > 0;
    public IReadOnlyList<StockAdjustmentMode> WoolAdjustmentModes { get; } = Enum.GetValues<StockAdjustmentMode>().ToList();
    public IReadOnlyList<StockAdjustmentMode> WoolDisplayModes { get; } = Enum.GetValues<StockAdjustmentMode>().ToList();
    public ProjectImageViewModel? SelectedImage =>
        SelectedImageIndex >= 0 && SelectedImageIndex < Images.Count ? Images[SelectedImageIndex] : null;
    public string ImagePositionDisplay => HasImages ? $"{SelectedImageIndex + 1} / {Images.Count}" : string.Empty;

    public IList<StatItem> PatternStats =>
    [
        new() { Label = "Documents", Value = (Pattern?.Documents.Count ?? 0).ToString("N0"), Unit = "x", IsFirst = true },
        new() { Label = "Projets liés", Value = (Pattern?.Projects.Count ?? 0).ToString("N0"), Unit = "x" }
    ];

    public IList<InfoItem> ProjectInfos =>
    [
        new() { Label = "Nom", Value = Name },
        new() { Label = "Status", Value = StatusDisplay },
        new() { Label = "Début", Value = FormatDate(BeginDate) },
        new() { Label = "Fin", Value = FormatDate(EndDate) },
        new() { Label = "Laines", Value = Wools.Count.ToString("N0") },
    ];

    public IList<InfoItem> PatternInfos =>
    [
        new() { Label = "Patron", Value = PatternName },
        new() { Label = "Type", Value = PatternTypeDisplay },
        new() { Label = "Origine", Value = Pattern?.IsPersonal == true ? "Personnel" : "Non personnel" },
        new() { Label = "Lien", Value = Pattern?.Url ?? "Aucun" },
        new() { Label = "Début", Value = FormatDate(Pattern?.BeginDate) },
        new() { Label = "Fin", Value = FormatDate(Pattern?.EndDate) },
    ];

    public void Load(Project project)
    {
        ProjectId = project.ProjectId;
        ApplyProject(project);
    }

    public override async void OnNavigatedTo()
    {
        RegisterRefresh(refreshService, RefreshScope.Projects | RefreshScope.Patterns | RefreshScope.Documents | RefreshScope.Wools, RefreshAsync);
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (ProjectId == 0)
            return;

        var result = await projectService.GetByIdAsync(ProjectId);
        if (result.Failed || result.Value is null)
        {
            ErrorMessage = result.Error ?? $"Le projet {ProjectId} est introuvable.";
            notifications.Error(ErrorMessage);
            return;
        }

        ApplyProject(result.Value);
    }

    private void ApplyProject(Project project)
    {
        _project = project;
        ErrorMessage = null;
        ProjectId = project.ProjectId;
        Name = project.Name;
        Status = project.Status;
        Note = project.Note;
        BeginDate = project.BeginDate;
        EndDate = project.EndDate;
        Pattern = project.Pattern;
        Wools = new ObservableCollection<Shared.Projects.ProjectWoolUsageViewModel>(
            project.Wools.Select(usage => new Shared.Projects.ProjectWoolUsageViewModel(
                usage,
                new AsyncRelayCommand(() => AddWoolUsageAsync(usage)))
            {
                DisplayMode = WoolDisplayMode
            }));
        Images = new ObservableCollection<ProjectImageViewModel>(
            project.Files
                .Where(document => documentFilePicker.IsSupportedFile(DocumentPickerMode.Images, document))
                .Select(image => new ProjectImageViewModel(image)));
        SelectedImageIndex = Images.Count == 0 ? -1 : Math.Clamp(SelectedImageIndex, 0, Images.Count - 1);

        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(NoteDisplay));
        OnPropertyChanged(nameof(PatternName));
        OnPropertyChanged(nameof(PatternTypeDisplay));
        OnPropertyChanged(nameof(PatternNoteDisplay));
        OnPropertyChanged(nameof(IsWishlist));
        OnPropertyChanged(nameof(IsInProgress));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(HasProjectActions));
        OnPropertyChanged(nameof(HasWools));
        OnPropertyChanged(nameof(HasImages));
        OnPropertyChanged(nameof(HasMultipleImages));
        OnPropertyChanged(nameof(SelectedImage));
        OnPropertyChanged(nameof(ImagePositionDisplay));
        OnPropertyChanged(nameof(ProjectInfos));
        OnPropertyChanged(nameof(PatternInfos));
        OnPropertyChanged(nameof(PatternStats));
    }

    partial void OnWoolAdjustmentQuantityChanged(double? value) =>
        OnPropertyChanged(nameof(CanAdjustWool));

    partial void OnWoolDisplayModeChanged(StockAdjustmentMode value)
    {
        foreach (var wool in Wools)
        {
            wool.DisplayMode = value;
        }
    }

    private async Task AddWoolUsageAsync(WoolUsage usage)
    {
        await AdjustWoolUsageAsync(usage);
    }

    private async Task AdjustWoolUsageAsync(WoolUsage usage)
    {
        if (WoolAdjustmentQuantity is null || WoolAdjustmentQuantity <= 0)
        {
            notifications.Error("Indiquez une quantité supérieure à zéro.");
            return;
        }

        var result = await stockService.AdjustWoolUsageAsync(new AdjustProjectWoolUsageRequest(
            ProjectId,
            usage.Wool.Id,
            WoolAdjustmentMode,
            true,
            WoolAdjustmentQuantity.Value,
            DeductWoolImmediately));
        if (result.Failed)
        {
            notifications.Error(result.Error ?? "Impossible de mettre à jour la laine utilisée.");
            return;
        }

    }

    private static string FormatDate(DateOnly? value) =>
        value is null ? "Aucune" : value.Value.ToString("dd/MM/yyyy");

    private async Task UpdateStatusAsync(Status status, DateOnly? beginDate = null, DateOnly? endDate = null)
    {
        IsBusy = true;
        try
        {
            var result = await projectService.UpdateAsync(new UpdateProjectRequest(
                ProjectId,
                Name,
                status,
                Note,
                beginDate ?? BeginDate,
                endDate ?? EndDate,
                Pattern?.Id,
                Wools.Select(w => w.Usage.Wool.Id).ToList()));

            if (result.Failed || result.Value is null)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? "Impossible de mettre à jour le projet.");
                return;
            }

            ApplyProject(result.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task StartProjectAsync() =>
        UpdateStatusAsync(Status.InProgress, DateOnly.FromDateTime(DateTime.Today));

    [RelayCommand]
    private Task PauseProjectAsync() =>
        UpdateStatusAsync(Status.Paused);

    [RelayCommand]
    private Task ResumeProjectAsync() =>
        UpdateStatusAsync(Status.InProgress);

    [RelayCommand]
    private void FinishProject() =>
        nav.NavigateTo<ProjectsFinishViewModel>(vm => vm.Load(ProjectId));

    [RelayCommand]
    private void OpenPattern()
    {
        if (Pattern is null)
            return;

        nav.NavigateTo<PatternsDetailViewModel>(vm => vm.Load(Pattern));
    }

    [RelayCommand]
    private void Edit()
    {
        if (_project is null)
            return;

        nav.NavigateTo<ProjectsFormViewModel>(vm => vm.InitEdit(_project));
    }

    partial void OnImagesChanged(ObservableCollection<ProjectImageViewModel> value)
    {
        OnPropertyChanged(nameof(HasImages));
        OnPropertyChanged(nameof(HasMultipleImages));
        OnPropertyChanged(nameof(SelectedImage));
        OnPropertyChanged(nameof(ImagePositionDisplay));
    }

    partial void OnSelectedImageIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedImage));
        OnPropertyChanged(nameof(ImagePositionDisplay));
    }

    [RelayCommand]
    private void PreviousImage()
    {
        if (Images.Count == 0)
            return;

        SelectedImageIndex = SelectedImageIndex <= 0 ? Images.Count - 1 : SelectedImageIndex - 1;
    }

    [RelayCommand]
    private void NextImage()
    {
        if (Images.Count == 0)
            return;

        SelectedImageIndex = SelectedImageIndex >= Images.Count - 1 ? 0 : SelectedImageIndex + 1;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsBusy = true;
        try
        {
            var result = await projectService.DeleteAsync(ProjectId);
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? "Impossible de supprimer le projet.");
                return;
            }

            notifications.Success("Le projet a été supprimé.");
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
