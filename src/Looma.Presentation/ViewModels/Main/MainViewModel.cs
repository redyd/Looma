// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.IServices;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Main;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IUpdaterService _updaterService;
    private readonly IUpdateInteractionService _updateInteraction;
    private readonly INotificationService _notifications;

    public SectionNavigationViewModel ProjectsSection { get; }
    public SectionNavigationViewModel StocksSection { get; }
    public SectionNavigationViewModel PatternsSection { get; }
    public SectionNavigationViewModel DocumentsSection { get; }
    public SectionNavigationViewModel StatisticsSection { get; }
    public SectionNavigationViewModel SettingsSection { get; }
    public INotificationService Notifications { get; }

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    public partial bool IsUpdatePromptVisible { get; set; }

    [ObservableProperty]
    public partial bool IsReleaseNotesVisible { get; set; }

    [ObservableProperty]
    public partial bool IsInstallingUpdate { get; set; }

    public string CurrentVersion => _updaterService.CurrentVersion;
    public string UpdateVersion => _updaterService.UpdateInformations?.Version ?? string.Empty;
    public string UpdateReleaseNotes => _updaterService.UpdateInformations?.ReleaseNotes ?? string.Empty;
    public string CurrentReleaseNotes => _updaterService.CurrentReleaseNotes;
    public int DownloadProgress => _updaterService.DownloadProgress;
    public bool HasDownloadProgress => _updaterService.Status is UpdateStatus.Downloading or UpdateStatus.Installing;
    public bool CanCloseUpdatePrompt => !IsInstallingUpdate;
    public bool CanConfirmUpdate => !IsInstallingUpdate;

    public MainViewModel(
        SectionNavigationViewModel projectsSection,
        SectionNavigationViewModel stocksSection,
        SectionNavigationViewModel patternsSection,
        SectionNavigationViewModel documentsSection,
        SectionNavigationViewModel statisticsSection,
        SectionNavigationViewModel settingsSection,
        INotificationService notifications,
        IUpdaterService updaterService,
        IUpdateInteractionService updateInteraction)
    {
        _updaterService = updaterService;
        _updateInteraction = updateInteraction;
        _notifications = notifications;
        PatternsSection = patternsSection;
        StocksSection = stocksSection;
        ProjectsSection = projectsSection;
        DocumentsSection = documentsSection;
        StatisticsSection = statisticsSection;
        SettingsSection = settingsSection;
        Notifications = notifications;
        SelectedTabIndex = 0;

        _updaterService.StateChanged += OnUpdaterStateChanged;
        _updateInteraction.UpdatePromptRequested += OnUpdatePromptRequested;
        _updateInteraction.CurrentReleaseNotesRequested += OnCurrentReleaseNotesRequested;
    }

    [RelayCommand]
    private void CloseUpdatePrompt()
    {
        if (IsInstallingUpdate)
            return;

        IsUpdatePromptVisible = false;
    }

    [RelayCommand]
    private async Task ConfirmUpdateAsync()
    {
        if (_updaterService.UpdateInformations is null)
            return;

        IsInstallingUpdate = true;
        OnPropertyChanged(nameof(CanCloseUpdatePrompt));
        OnPropertyChanged(nameof(CanConfirmUpdate));

        await _updaterService.UpdateAsync();

        if (_updaterService.Status == UpdateStatus.Error)
        {
            IsInstallingUpdate = false;
            OnPropertyChanged(nameof(CanCloseUpdatePrompt));
            OnPropertyChanged(nameof(CanConfirmUpdate));
            _notifications.Error(_updaterService.ErrorMessage ?? Translation["Update_Notifications_UnableToInstallUpdate"]);
        }
    }

    [RelayCommand]
    private async Task CloseReleaseNotesAsync()
    {
        IsReleaseNotesVisible = false;
        await _updaterService.MarkCurrentReleaseNotesAsShownAsync();
    }

    private void OnUpdatePromptRequested(object? sender, EventArgs e)
    {
        RunOnUiThread(() =>
        {
            if (_updaterService.UpdateInformations is not null)
            {
                IsUpdatePromptVisible = true;
            }
        });
    }

    private void OnCurrentReleaseNotesRequested(object? sender, EventArgs e)
    {
        RunOnUiThread(() =>
        {
            OnPropertyChanged(nameof(CurrentReleaseNotes));
            IsReleaseNotesVisible = true;
        });
    }

    private void OnUpdaterStateChanged(object? sender, EventArgs e)
    {
        RunOnUiThread(() =>
        {
            if (IsInstallingUpdate && _updaterService.Status == UpdateStatus.Idle)
            {
                IsInstallingUpdate = false;
                IsUpdatePromptVisible = false;
                OnPropertyChanged(nameof(CanCloseUpdatePrompt));
                OnPropertyChanged(nameof(CanConfirmUpdate));
            }

            OnPropertyChanged(nameof(CurrentVersion));
            OnPropertyChanged(nameof(UpdateVersion));
            OnPropertyChanged(nameof(UpdateReleaseNotes));
            OnPropertyChanged(nameof(CurrentReleaseNotes));
            OnPropertyChanged(nameof(DownloadProgress));
            OnPropertyChanged(nameof(HasDownloadProgress));
        });
    }

    public void Dispose()
    {
        _updaterService.StateChanged -= OnUpdaterStateChanged;
        _updateInteraction.UpdatePromptRequested -= OnUpdatePromptRequested;
        _updateInteraction.CurrentReleaseNotesRequested -= OnCurrentReleaseNotesRequested;
    }
}
