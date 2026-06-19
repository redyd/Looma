namespace Looma.Domain.IServices;

public interface IUpdateMockService
{
    bool IsUpdateMockEnabled { get; }
    string MockCurrentVersion { get; set; }
    string MockUpdateVersion { get; set; }
    string MockReleaseNotes { get; set; }
    bool CanSimulateRestart { get; }

    Task PublishMockUpdateAsync();
    Task SimulateRestartAsync();
}
