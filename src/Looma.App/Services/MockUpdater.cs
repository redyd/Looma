// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

using System;
using System.Threading.Tasks;
using Looma.Domain.Core;
using Looma.Domain.IServices;

namespace Looma.App.Services;

public sealed class MockUpdater(ISettingsService settingsService) : IUpdaterService
{
    private string _currentVersion = "0.2.1";
    private string _publishedVersion = "0.2.2";
    private string _releaseNotes = "## v0.2.2\n\n- Note de version simulée.\n- Téléchargement testable sans release GitHub.";
    private int _checkDelayMilliseconds = 800;
    private int _downloadDelayMilliseconds = 2500;

    public event EventHandler? StateChanged;

    public UpdateStatus Status { get; private set; } = UpdateStatus.Idle;
    public UpdateChannel Channel { get; } = UpdateChannel.Stable;
    public string CurrentVersion => _currentVersion;
    public string CurrentReleaseNotes { get; private set; } = string.Empty;
    public int DownloadProgress { get; private set; }
    public string? ErrorMessage { get; private set; }
    public UpdateInformations? UpdateInformations { get; private set; }

    public string MockCurrentVersion
    {
        get => _currentVersion;
        set
        {
            _currentVersion = NormalizeVersion(value);
            UpdateAvailableInformation();
            OnStateChanged();
        }
    }

    public string MockPublishedVersion
    {
        get => _publishedVersion;
        set
        {
            _publishedVersion = NormalizeVersion(value);
            UpdateAvailableInformation();
            OnStateChanged();
        }
    }

    public string MockReleaseNotes
    {
        get => _releaseNotes;
        set
        {
            _releaseNotes = value;
            UpdateAvailableInformation();
            OnStateChanged();
        }
    }

    public int MockCheckDelayMilliseconds
    {
        get => _checkDelayMilliseconds;
        set => _checkDelayMilliseconds = Math.Max(0, value);
    }

    public int MockDownloadDelayMilliseconds
    {
        get => _downloadDelayMilliseconds;
        set => _downloadDelayMilliseconds = Math.Max(0, value);
    }

    public async Task CheckForUpdatesAsync(bool silent = false)
    {
        if (Status is UpdateStatus.Checking or UpdateStatus.Downloading or UpdateStatus.Installing)
            return;

        ErrorMessage = null;
        DownloadProgress = 0;
        SetStatus(UpdateStatus.Checking);
        await Task.Delay(MockCheckDelayMilliseconds);

        UpdateAvailableInformation();
        SetStatus(UpdateInformations is null ? UpdateStatus.Idle : UpdateStatus.Available);
    }

    public async Task UpdateAsync(IProgress<int>? progress = null)
    {
        if (Status is UpdateStatus.Downloading or UpdateStatus.Installing || UpdateInformations is null)
            return;

        ErrorMessage = null;
        DownloadProgress = 0;
        SetStatus(UpdateStatus.Downloading);

        const int steps = 10;
        var stepDelay = MockDownloadDelayMilliseconds / steps;
        for (var step = 1; step <= steps; step++)
        {
            if (stepDelay > 0)
            {
                await Task.Delay(stepDelay);
            }

            DownloadProgress = step * 10;
            progress?.Report(DownloadProgress);
            OnStateChanged();
        }

        var installedVersion = UpdateInformations.Version;
        var installedNotes = UpdateInformations.ReleaseNotes;
        await settingsService.SetVersionAsync(installedVersion);
        await settingsService.SetReleaseNotesAsync(installedVersion, installedNotes);
        await settingsService.SetReleaseNotesShownAsync(installedVersion, false);

        _currentVersion = installedVersion;
        CurrentReleaseNotes = installedNotes;
        UpdateInformations = null;
        SetStatus(UpdateStatus.Idle);
    }

    public async Task<bool> ShouldShowCurrentReleaseNotesAsync()
    {
        var result = await settingsService.GetReleaseNotesAsync(CurrentVersion);
        CurrentReleaseNotes = result.Succeeded ? result.Value ?? string.Empty : string.Empty;

        if (string.IsNullOrWhiteSpace(CurrentReleaseNotes))
            return false;

        var shown = await settingsService.GetReleaseNotesShownAsync(CurrentVersion);
        return shown.Succeeded && !shown.Value;
    }

    public Task MarkCurrentReleaseNotesAsShownAsync() =>
        settingsService.SetReleaseNotesShownAsync(CurrentVersion, true);

    private void UpdateAvailableInformation()
    {
        UpdateInformations = string.Equals(_currentVersion, _publishedVersion, StringComparison.OrdinalIgnoreCase)
            ? null
            : new UpdateInformations
            {
                Version = _publishedVersion,
                ReleaseNotes = _releaseNotes,
                Channel = "mock"
            };
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
