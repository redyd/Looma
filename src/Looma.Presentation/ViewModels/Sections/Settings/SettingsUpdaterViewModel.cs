// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.IServices;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Settings;

public partial class SettingsUpdaterViewModel(
    INotificationService notifications,
    IUpdaterService updaterService,
    IUpdateInteractionService updateInteraction)
    : ViewModelBase
{
    private bool _isListeningUpdater;
    private bool _isCheckingForUpdates;

    public string InstalledVersion => $"v.{updaterService.CurrentVersion}";
    public bool IsCheckingForUpdates => _isCheckingForUpdates || updaterService.Status == UpdateStatus.Checking;
    public bool IsUpdateAvailable => updaterService.Status == UpdateStatus.Available;
    private bool CanCheckForUpdates() => true;
    public string UpdateIconKind => IsCheckingForUpdates
        ? "LoaderCircle"
        : IsUpdateAvailable
            ? "Download"
            : "Search";
    public string UpdateToolTip => IsUpdateAvailable
        ? Translation.Format("Update_InstallVersion", updaterService.UpdateInformations?.Version ?? string.Empty)
        : Translation["Update_CheckForUpdates"];

    public void OnNavigatedTo()
    {
        if (!_isListeningUpdater)
        {
            updaterService.StateChanged += OnUpdaterStateChanged;
            _isListeningUpdater = true;
        }

        RaiseUpdatePropertiesChanged();
    }

    public void OnNavigatedFrom()
    {
        if (!_isListeningUpdater)
            return;

        updaterService.StateChanged -= OnUpdaterStateChanged;
        _isListeningUpdater = false;
    }

    [RelayCommand]
    private void ShowCurrentReleaseNotes()
    {
        updateInteraction.RequestCurrentReleaseNotes();
    }

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdatesAsync()
    {
        if (updaterService.Status == UpdateStatus.Checking)
            return;

        if (updaterService.Status == UpdateStatus.Available)
        {
            updateInteraction.RequestUpdatePrompt();
            return;
        }

        _isCheckingForUpdates = true;
        RaiseUpdatePropertiesChanged();
        await Task.Yield();

        try
        {
            await updaterService.CheckForUpdatesAsync();
        }
        finally
        {
            _isCheckingForUpdates = false;
            RaiseUpdatePropertiesChanged();
        }

        if (updaterService.Status == UpdateStatus.Available)
        {
            updateInteraction.RequestUpdatePrompt();
            return;
        }

        if (updaterService.Status == UpdateStatus.Error)
        {
            notifications.Error(updaterService.ErrorMessage ?? Translation["Update_Notifications_UnableToCheckUpdates"]);
            return;
        }

        notifications.Info(Translation["Update_Notifications_LoomaIsUpToDate"]);
    }

    private void OnUpdaterStateChanged(object? sender, EventArgs e) =>
        RunOnUiThread(RaiseUpdatePropertiesChanged);

    private void RaiseUpdatePropertiesChanged()
    {
        OnPropertyChanged(nameof(InstalledVersion));
        OnPropertyChanged(nameof(IsCheckingForUpdates));
        OnPropertyChanged(nameof(IsUpdateAvailable));
        OnPropertyChanged(nameof(UpdateIconKind));
        OnPropertyChanged(nameof(UpdateToolTip));
    }
}
