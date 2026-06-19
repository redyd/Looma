using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Looma.App.Services;

public sealed class VelopackUpdateManagerAdapter(string channel) : IUpdateManagerAdapter
{
    private readonly UpdateManager _updater = new(
        new GithubSource("https://github.com/redyd/Looma", null, false),
        new UpdateOptions { ExplicitChannel = channel }
    );

    public bool IsInstalled => _updater.IsInstalled;

    public async Task<AvailableUpdate?> CheckForUpdatesAsync()
    {
        var update = await _updater.CheckForUpdatesAsync();
        return update is null
            ? null
            : new AvailableUpdate(
                update,
                update.TargetFullRelease.Version.ToString(),
                update.TargetFullRelease.NotesMarkdown ?? string.Empty);
    }

    public Task DownloadUpdatesAsync(AvailableUpdate update, Action<int> progress) =>
        _updater.DownloadUpdatesAsync((UpdateInfo)update.NativeUpdate, progress);

    public void ApplyUpdatesAndRestart(AvailableUpdate update) =>
        _updater.ApplyUpdatesAndRestart((UpdateInfo)update.NativeUpdate);
}
