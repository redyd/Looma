using Looma.Domain.Core;

namespace Looma.Domain.IServices;

public interface IUpdaterService
{
    event EventHandler? StateChanged;

    UpdateStatus Status { get; }
    UpdateChannel Channel { get; }
    string CurrentVersion { get; }
    string CurrentReleaseNotes { get; }
    int DownloadProgress { get; }
    string? ErrorMessage { get; }
    UpdateInformations? UpdateInformations { get; }

    Task CheckForUpdatesAsync(bool silent = false);
    Task UpdateAsync(IProgress<int>? progress = null);
    Task<bool> ShouldShowCurrentReleaseNotesAsync();
    Task MarkCurrentReleaseNotesAsShownAsync();
}
