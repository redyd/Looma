namespace Looma.App.Services;

public sealed class AvailableUpdate(
    object nativeUpdate,
    string version,
    string releaseNotes)
{
    public object NativeUpdate { get; } = nativeUpdate;
    public string Version { get; } = version;
    public string ReleaseNotes { get; } = releaseNotes;
}
