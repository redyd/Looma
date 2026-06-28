// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

using Looma.App.Services;
using Looma.Domain.Core;
using Looma.Domain.IServices;

namespace Looma.App.Tests.TestSupport;

internal sealed class FakeUpdateManagerAdapter : IUpdateManagerAdapter
{
    public bool IsInstalled { get; set; } = true;
    public AvailableUpdate? NextUpdate { get; set; }
    public Exception? CheckException { get; set; }
    public Exception? DownloadException { get; set; }
    public TimeSpan DownloadDelay { get; set; }
    public List<int> ProgressValues { get; set; } = [];

    public int CheckCalls { get; private set; }
    public int DownloadCalls { get; private set; }
    public int ApplyCalls { get; private set; }

    public Task<AvailableUpdate?> CheckForUpdatesAsync()
    {
        CheckCalls++;
        if (CheckException is not null)
            throw CheckException;

        return Task.FromResult(NextUpdate);
    }

    public async Task DownloadUpdatesAsync(AvailableUpdate update, Action<int> progress)
    {
        DownloadCalls++;
        if (DownloadException is not null)
            throw DownloadException;

        if (DownloadDelay > TimeSpan.Zero)
            await Task.Delay(DownloadDelay);

        foreach (var value in ProgressValues)
        {
            progress(value);
        }
    }

    public void ApplyUpdatesAndRestart(AvailableUpdate update) => ApplyCalls++;
}

internal sealed class FakeSettingsService : ISettingsService
{
    public string? SelectedLanguage { get; private set; }
    public string? Version { get; private set; }
    public Dictionary<string, string> ReleaseNotes { get; } = [];
    public Dictionary<string, bool> ReleaseNotesShown { get; } = [];

    public Task<ResultT<string?>> GetSelectedLanguageAsync() =>
        Task.FromResult(ResultT<string?>.Ok(SelectedLanguage));

    public Task<Result> SetSelectedLanguageAsync(string culture)
    {
        SelectedLanguage = culture;
        return Task.FromResult(Result.Ok());
    }

    public Task<ResultT<string?>> GetVersionAsync() => Task.FromResult(ResultT<string?>.Ok(Version));

    public Task<Result> SetVersionAsync(string version)
    {
        Version = version;
        return Task.FromResult(Result.Ok());
    }

    public Task<ResultT<string?>> GetReleaseNotesAsync(string version) =>
        Task.FromResult(ResultT<string?>.Ok(ReleaseNotes.GetValueOrDefault(version)));

    public Task<Result> SetReleaseNotesAsync(string version, string releaseNotes)
    {
        ReleaseNotes[version] = releaseNotes;
        return Task.FromResult(Result.Ok());
    }

    public Task<ResultT<bool>> GetReleaseNotesShownAsync(string version) =>
        Task.FromResult(ResultT<bool>.Ok(ReleaseNotesShown.GetValueOrDefault(version)));

    public Task<Result> SetReleaseNotesShownAsync(string version, bool shown)
    {
        ReleaseNotesShown[version] = shown;
        return Task.FromResult(Result.Ok());
    }
}
