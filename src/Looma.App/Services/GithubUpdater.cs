using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Looma.Domain.Core;
using Looma.Domain.IServices;

namespace Looma.App.Services;

public class GithubUpdater : IUpdaterService
{
    private readonly ISettingsService _settingsService;
    private readonly IUpdateManagerAdapter _updater;
    private readonly string _channelName;

    private AvailableUpdate? _availableUpdate;

    public event EventHandler? StateChanged;

    public UpdateStatus Status { get; private set; } = UpdateStatus.Idle;
    public UpdateChannel Channel { get; } = UpdateChannel.Stable;
    public string CurrentVersion { get; }
    public string CurrentReleaseNotes { get; private set; } = string.Empty;
    public int DownloadProgress { get; private set; }
    public string? ErrorMessage { get; private set; }
    public UpdateInformations? UpdateInformations { get; private set; }

    public GithubUpdater(ISettingsService settingsService)
        : this(settingsService, null, null)
    {
    }

    public GithubUpdater(
        ISettingsService settingsService,
        IUpdateManagerAdapter? updater,
        string? currentVersion)
    {
        _settingsService = settingsService;
        _channelName = GetVelopackChannel(Channel);
        CurrentVersion = currentVersion ?? GetCurrentVersion();
        _updater = updater ?? new VelopackUpdateManagerAdapter(_channelName);

        _ = LoadCurrentReleaseNotesAsync();
    }

    public async Task CheckForUpdatesAsync(bool silent = false)
    {
        if (Status is UpdateStatus.Checking or UpdateStatus.Downloading or UpdateStatus.Installing)
            return;

        SetStatus(UpdateStatus.Checking);
        ErrorMessage = null;

        try
        {
            if (!_updater.IsInstalled)
            {
                SetStatus(UpdateStatus.Idle);
                return;
            }

            var newVersion = await _updater.CheckForUpdatesAsync();
            if (newVersion is null)
            {
                UpdateInformations = null;
                _availableUpdate = null;
                SetStatus(UpdateStatus.Idle);
                return;
            }

            UpdateInformations = new UpdateInformations
            {
                Version = newVersion.Version,
                ReleaseNotes = newVersion.ReleaseNotes,
                Channel = _channelName
            };

            _availableUpdate = newVersion;
            SetStatus(UpdateStatus.Available);
        }
        catch (Exception ex)
        {
            ErrorMessage = silent ? null : ex.Message;
            SetStatus(silent ? UpdateStatus.Idle : UpdateStatus.Error);
        }
    }

    public async Task UpdateAsync(IProgress<int>? progress = null)
    {
        if (_availableUpdate is null || UpdateInformations is null)
        {
            await CheckForUpdatesAsync();
            if (_availableUpdate is null || UpdateInformations is null)
            {
                return;
            }
        }

        try
        {
            ErrorMessage = null;
            DownloadProgress = 0;
            SetStatus(UpdateStatus.Downloading);

            await _updater.DownloadUpdatesAsync(_availableUpdate, value =>
            {
                DownloadProgress = value;
                progress?.Report(value);
                OnStateChanged();
            });

            await _settingsService.SetVersionAsync(UpdateInformations.Version);
            await _settingsService.SetReleaseNotesAsync(UpdateInformations.Version, UpdateInformations.ReleaseNotes);
            await _settingsService.SetReleaseNotesShownAsync(UpdateInformations.Version, false);

            SetStatus(UpdateStatus.Installing);
            _updater.ApplyUpdatesAndRestart(_availableUpdate);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            SetStatus(UpdateStatus.Error);
        }
    }

    public async Task<bool> ShouldShowCurrentReleaseNotesAsync()
    {
        await LoadCurrentReleaseNotesAsync();

        if (string.IsNullOrWhiteSpace(CurrentReleaseNotes))
            return false;

        var result = await _settingsService.GetReleaseNotesShownAsync(CurrentVersion);
        return result.Succeeded && !result.Value;
    }

    public async Task MarkCurrentReleaseNotesAsShownAsync() =>
        await _settingsService.SetReleaseNotesShownAsync(CurrentVersion, true);

    private async Task LoadCurrentReleaseNotesAsync()
    {
        var result = await _settingsService.GetReleaseNotesAsync(CurrentVersion);
        CurrentReleaseNotes = result.Succeeded
            ? result.Value ?? string.Empty
            : string.Empty;
        OnStateChanged();
    }

    private void SetStatus(UpdateStatus status)
    {
        Status = status;
        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private static string GetCurrentVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private static string GetVelopackChannel(UpdateChannel channel)
    {
        var suffix = channel == UpdateChannel.Stable
            ? string.Empty
            : $"-{channel.ToString().ToLowerInvariant()}";

        var platform = OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsLinux() ? "linux"
            : RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64"
            : "osx-x64";

        return $"{platform}{suffix}";
    }
}
