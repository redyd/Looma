// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

using System;
using System.Threading.Tasks;

using Looma.Domain.Core;
using Looma.Domain.IServices;

namespace Looma.App.Services;

public sealed class MockUpdater(ISettingsService settingsService) : IUpdaterService, IUpdateMockService
{
    private string? _pendingInstalledVersion;
    private string _currentVersion = "0.2.1";

    public event EventHandler? StateChanged;

    public UpdateStatus Status { get; private set; } = UpdateStatus.Idle;
    public UpdateChannel Channel { get; } = UpdateChannel.Stable;
    public string CurrentVersion => _currentVersion;
    public string CurrentReleaseNotes { get; private set; } = string.Empty;
    public int DownloadProgress { get; private set; }
    public string? ErrorMessage { get; private set; }
    public UpdateInformations? UpdateInformations { get; private set; }

    public bool IsUpdateMockEnabled => true;
    public string MockCurrentVersion
    {
        get => _currentVersion;
        set
        {
            _currentVersion = NormalizeVersion(value);
            OnStateChanged();
        }
    }
    public string MockUpdateVersion { get; set; } = "0.2.2";
    public string MockReleaseNotes { get; set; } = "## v0.2.2\n\n- Note de version simulée.\n- Téléchargement et redémarrage testables sans release GitHub.";
    public bool CanSimulateRestart => _pendingInstalledVersion is not null;

    public async Task CheckForUpdatesAsync(bool silent = false)
    {
        if (Status is UpdateStatus.Downloading or UpdateStatus.Installing)
            return;

        SetStatus(UpdateStatus.Checking);
        await Task.Delay(150);
        SetStatus(UpdateInformations is null ? UpdateStatus.Idle : UpdateStatus.Available);
    }

    public async Task UpdateAsync(IProgress<int>? progress = null)
    {
        if (UpdateInformations is null)
            return;

        ErrorMessage = null;
        DownloadProgress = 0;
        SetStatus(UpdateStatus.Downloading);

        foreach (var value in new[] { 10, 35, 70, 100 })
        {
            await Task.Delay(120);
            DownloadProgress = value;
            progress?.Report(value);
            OnStateChanged();
        }

        await settingsService.SetVersionAsync(UpdateInformations.Version);
        await settingsService.SetReleaseNotesAsync(UpdateInformations.Version, UpdateInformations.ReleaseNotes);
        await settingsService.SetReleaseNotesShownAsync(UpdateInformations.Version, false);

        _pendingInstalledVersion = UpdateInformations.Version;
        SetStatus(UpdateStatus.Installing);
    }

    public async Task<bool> ShouldShowCurrentReleaseNotesAsync()
    {
        await LoadCurrentReleaseNotesAsync();

        if (string.IsNullOrWhiteSpace(CurrentReleaseNotes))
            return false;

        var result = await settingsService.GetReleaseNotesShownAsync(CurrentVersion);
        return result.Succeeded && !result.Value;
    }

    public Task MarkCurrentReleaseNotesAsShownAsync() =>
        settingsService.SetReleaseNotesShownAsync(CurrentVersion, true);

    public Task PublishMockUpdateAsync()
    {
        ErrorMessage = null;
        DownloadProgress = 0;
        UpdateInformations = new UpdateInformations
        {
            Version = NormalizeVersion(MockUpdateVersion),
            ReleaseNotes = MockReleaseNotes,
            Channel = "mock"
        };
        SetStatus(UpdateStatus.Available);
        return Task.CompletedTask;
    }

    public async Task SimulateRestartAsync()
    {
        if (_pendingInstalledVersion is null)
            return;

        _currentVersion = _pendingInstalledVersion;
        _pendingInstalledVersion = null;
        UpdateInformations = null;
        DownloadProgress = 0;
        SetStatus(UpdateStatus.Idle);
        await LoadCurrentReleaseNotesAsync();
    }

    private async Task LoadCurrentReleaseNotesAsync()
    {
        var result = await settingsService.GetReleaseNotesAsync(CurrentVersion);
        CurrentReleaseNotes = result.Succeeded ? result.Value ?? string.Empty : string.Empty;
        OnStateChanged();
    }

    private void SetStatus(UpdateStatus status)
    {
        Status = status;
        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private static string NormalizeVersion(string version)
    {
        var normalized = string.IsNullOrWhiteSpace(version) ? "0.0.0" : version.Trim();
        return normalized.StartsWith('v') ? normalized[1..] : normalized;
    }
}
