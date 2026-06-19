using System;
using System.Threading.Tasks;

namespace Looma.App.Services;

public interface IUpdateManagerAdapter
{
    bool IsInstalled { get; }

    Task<AvailableUpdate?> CheckForUpdatesAsync();
    Task DownloadUpdatesAsync(AvailableUpdate update, Action<int> progress);
    void ApplyUpdatesAndRestart(AvailableUpdate update);
}
