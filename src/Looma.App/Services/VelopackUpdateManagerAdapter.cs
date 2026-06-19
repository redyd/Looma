// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.

using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Looma.App.Services;

public sealed class VelopackUpdateManagerAdapter(string channel) : IUpdateManagerAdapter
{
    private const string RepoUrl = "https://github.com/redyd/Looma";

    public bool IsInstalled => CreateUpdateManager().IsInstalled;

    public async Task<AvailableUpdate?> CheckForUpdatesAsync()
    {
        var update = await CreateUpdateManager().CheckForUpdatesAsync();
        return update is null
            ? null
            : new AvailableUpdate(
                update,
                update.TargetFullRelease.Version.ToString(),
                update.TargetFullRelease.NotesMarkdown ?? string.Empty);
    }

    public Task DownloadUpdatesAsync(AvailableUpdate update, Action<int> progress) =>
        CreateUpdateManager().DownloadUpdatesAsync((UpdateInfo)update.NativeUpdate, progress);

    public void ApplyUpdatesAndRestart(AvailableUpdate update) =>
        CreateUpdateManager().ApplyUpdatesAndRestart((UpdateInfo)update.NativeUpdate);

    private UpdateManager CreateUpdateManager() =>
        new(new GithubSource(RepoUrl, null, false), new UpdateOptions { ExplicitChannel = channel });
}
