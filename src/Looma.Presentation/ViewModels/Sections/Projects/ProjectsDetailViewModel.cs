// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.IServices;
using Looma.Domain.Refresh;
using Looma.Domain.Request;
using Looma.Presentation.Navigation;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
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
    private bool _isListeningTranslation;

    public ProjectDetailDisplayViewModel Display { get; } = new();

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }
    [ObservableProperty]
    public partial double? WoolAdjustmentQuantity { get; set; }
    [ObservableProperty]
    public partial string WoolAdjustmentQuantityText { get; set; } = string.Empty;
    [ObservableProperty]
    public partial StockAdjustmentMode WoolAdjustmentMode { get; set; } = StockAdjustmentMode.ByBall;
    [ObservableProperty]
    public partial StockAdjustmentMode WoolDisplayMode { get; set; } = StockAdjustmentMode.ByBall;
    [ObservableProperty]
    public partial bool DeductWoolImmediately { get; set; }

    public bool CanAdjustWool => WoolAdjustmentQuantity > 0;
    public IReadOnlyList<StockAdjustmentMode> WoolAdjustmentModes { get; } = Enum.GetValues<StockAdjustmentMode>().ToList();
    public IReadOnlyList<StockAdjustmentMode> WoolDisplayModes { get; } = Enum.GetValues<StockAdjustmentMode>().ToList();

    public void Load(Project project)
    {
        ApplyProject(project);
    }

    public override async void OnNavigatedTo()
    {
        RegisterRefresh(refreshService, RefreshScope.Projects | RefreshScope.Patterns | RefreshScope.Documents | RefreshScope.Wools, RefreshAsync);
        RegisterTranslationRefresh();
        await RefreshAsync();
    }

    protected override void OnDestroy()
    {
        if (_isListeningTranslation)
        {
            Translation.PropertyChanged -= OnTranslationChanged;
            _isListeningTranslation = false;
        }

        base.OnDestroy();
    }

    private void RegisterTranslationRefresh()
    {
        if (_isListeningTranslation)
            return;

        Translation.PropertyChanged += OnTranslationChanged;
        _isListeningTranslation = true;
    }

    private void OnTranslationChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(WoolAdjustmentModes));
        OnPropertyChanged(nameof(WoolDisplayModes));
        RefreshProjectTranslations();
    }

    private async Task RefreshAsync()
    {
        if (Display.ProjectId == 0)
            return;

        var result = await projectService.GetByIdAsync(Display.ProjectId);
        if (result.Failed || result.Value is null)
        {
            ErrorMessage = result.Error ?? Translation.Format("Projects_Errors_ProjectNotFound", Display.ProjectId);
            notifications.Error(ErrorMessage);
            return;
        }

        ApplyProject(result.Value);
    }

    private void ApplyProject(Project project)
    {
        _project = project;
        ErrorMessage = null;

        RefreshProjectDisplay(project);
        RefreshProjectWools(project);
        RefreshProjectImages(project);
    }

    public void RefreshProjectDisplay(Project project) => Display.RefreshProject(project);

    public void RefreshProjectWools(Project project)
    {
        var wools = new ObservableCollection<ProjectWoolUsageViewModel>(
            project.Wools.Select(usage => new ProjectWoolUsageViewModel(
                usage,
                new AsyncRelayCommand(() => AddWoolUsageAsync(usage)))
            {
                DisplayMode = WoolDisplayMode
            }));

        Display.RefreshWools(wools);
    }

    public void RefreshProjectImages(Project project)
    {
        var images = new ObservableCollection<ProjectImageViewModel>(
            project.Files
                .Where(document => documentFilePicker.IsSupportedFile(DocumentPickerMode.Images, document))
                .Select(image => new ProjectImageViewModel(image)));

        Display.RefreshImages(images);
    }

    public void RefreshProjectTranslations() => Display.RefreshTranslations();

    partial void OnWoolAdjustmentQuantityChanged(double? value) =>
        OnPropertyChanged(nameof(CanAdjustWool));

    partial void OnWoolAdjustmentQuantityTextChanged(string value)
    {
        WoolAdjustmentQuantity = TryParseQuantity(value, out var quantity)
            ? quantity
            : null;
    }

    partial void OnWoolDisplayModeChanged(StockAdjustmentMode value)
    {
        foreach (var wool in Display.Wools)
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
            notifications.Error(Translation["Common_Errors_QuantityGreaterThanZero"]);
            return;
        }

        var result = await stockService.AdjustWoolUsageAsync(new AdjustProjectWoolUsageRequest(
            Display.ProjectId,
            usage.Wool.Id,
            WoolAdjustmentMode,
            true,
            WoolAdjustmentQuantity.Value,
            DeductWoolImmediately));
        if (result.Failed)
        {
            notifications.Error(result.Error ?? Translation["Projects_Notifications_UnableToUpdateUsedWool"]);
            return;
        }

        WoolAdjustmentQuantityText = string.Empty;
        WoolAdjustmentQuantity = null;
    }

    private static bool TryParseQuantity(string? value, out double quantity)
    {
        quantity = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out quantity)
               || double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out quantity);
    }

    private async Task UpdateStatusAsync(Status status, DateOnly? beginDate = null, DateOnly? endDate = null)
    {
        IsBusy = true;
        try
        {
            var result = await projectService.UpdateAsync(new UpdateProjectRequest(
                Display.ProjectId,
                Display.Name,
                status,
                Display.Note,
                beginDate ?? Display.BeginDate,
                endDate ?? Display.EndDate,
                Display.Pattern?.Id,
                Display.Wools.Select(w => w.Usage.Wool.Id).ToList()));

            if (result.Failed || result.Value is null)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? Translation["Projects_Notifications_UnableToUpdateProject"]);
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
    private async Task SaveNoteAsync()
    {
        IsBusy = true;
        try
        {
            var result = await projectService.UpdateAsync(new UpdateProjectRequest(
                Display.ProjectId,
                Display.Name,
                Display.Status,
                Display.Note,
                Display.BeginDate,
                Display.EndDate,
                Display.Pattern?.Id,
                Display.Wools.Select(w => w.Usage.Wool.Id).ToList()));

            if (result.Failed || result.Value is null)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? Translation["Common_Notifications_UnableToUpdateNote"]);
                return;
            }

            ApplyProject(result.Value);
            notifications.Success(Translation["Common_Notifications_NoteUpdated"]);
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
        nav.NavigateTo<ProjectsFinishViewModel>(vm => vm.Load(Display.ProjectId));

    [RelayCommand]
    private void OpenPattern()
    {
        if (Display.Pattern is not null)
        {
            nav.NavigateTo<PatternsDetailViewModel>(vm => vm.Load(Display.Pattern));
            return;
        }

        Edit();
    }

    [RelayCommand]
    private void Edit()
    {
        if (_project is null)
            return;

        nav.NavigateTo<ProjectsFormViewModel>(vm => vm.InitEdit(_project));
    }

    [RelayCommand]
    private void PreviousImage()
    {
        if (Display.Images.Count == 0)
            return;

        Display.SelectedImageIndex = Display.SelectedImageIndex <= 0 ? Display.Images.Count - 1 : Display.SelectedImageIndex - 1;
    }

    [RelayCommand]
    private void NextImage()
    {
        if (Display.Images.Count == 0)
            return;

        Display.SelectedImageIndex = Display.SelectedImageIndex >= Display.Images.Count - 1 ? 0 : Display.SelectedImageIndex + 1;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        IsBusy = true;
        try
        {
            var result = await projectService.DeleteAsync(Display.ProjectId);
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                notifications.Error(result.Error ?? Translation["Projects_Notifications_UnableToDeleteProject"]);
                return;
            }

            notifications.Success(Translation["Projects_Notifications_ProjectDeleted"]);
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
