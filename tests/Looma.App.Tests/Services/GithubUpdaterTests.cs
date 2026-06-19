using Looma.App.Services;
using Looma.App.Tests.TestSupport;
using Looma.Domain.Core;

namespace Looma.App.Tests.Services;

public sealed class GithubUpdaterTests
{
    [Fact]
    public async Task CheckForUpdates_WhenAppIsNotInstalled_DoesNotQueryReleaseSource()
    {
        var adapter = new FakeUpdateManagerAdapter { IsInstalled = false };
        var sut = CreateUpdater(adapter);

        await sut.CheckForUpdatesAsync();

        adapter.CheckCalls.Should().Be(0);
        sut.Status.Should().Be(UpdateStatus.Idle);
        sut.UpdateInformations.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdates_WhenNoUpdateAvailable_ReturnsToIdle()
    {
        var adapter = new FakeUpdateManagerAdapter { NextUpdate = null };
        var sut = CreateUpdater(adapter);

        await sut.CheckForUpdatesAsync();

        adapter.CheckCalls.Should().Be(1);
        sut.Status.Should().Be(UpdateStatus.Idle);
        sut.UpdateInformations.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdates_WhenUpdateAvailable_PublishesUpdateInformation()
    {
        var adapter = new FakeUpdateManagerAdapter
        {
            NextUpdate = new AvailableUpdate(new object(), "2.1.0", "## Notes")
        };
        var sut = CreateUpdater(adapter);

        await sut.CheckForUpdatesAsync();

        sut.Status.Should().Be(UpdateStatus.Available);
        sut.UpdateInformations.Should().BeEquivalentTo(new
        {
            Version = "2.1.0",
            ReleaseNotes = "## Notes"
        });
    }

    [Fact]
    public async Task CheckForUpdates_WhenManualCheckFails_ExposesError()
    {
        var adapter = new FakeUpdateManagerAdapter
        {
            CheckException = new InvalidOperationException("network down")
        };
        var sut = CreateUpdater(adapter);

        await sut.CheckForUpdatesAsync();

        sut.Status.Should().Be(UpdateStatus.Error);
        sut.ErrorMessage.Should().Be("network down");
    }

    [Fact]
    public async Task CheckForUpdates_WhenSilentCheckFails_ReturnsToIdleWithoutMessage()
    {
        var adapter = new FakeUpdateManagerAdapter
        {
            CheckException = new InvalidOperationException("network down")
        };
        var sut = CreateUpdater(adapter);

        await sut.CheckForUpdatesAsync(silent: true);

        sut.Status.Should().Be(UpdateStatus.Idle);
        sut.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_DownloadsStoresReleaseNotesAndAppliesRestart()
    {
        var settings = new FakeSettingsService();
        var adapter = new FakeUpdateManagerAdapter
        {
            NextUpdate = new AvailableUpdate(new object(), "2.1.0", "## Notes"),
            ProgressValues = [10, 60, 100]
        };
        var sut = CreateUpdater(adapter, settings);
        var reportedProgress = new List<int>();
        var progress = new InlineProgress<int>(reportedProgress.Add);

        await sut.CheckForUpdatesAsync();
        await sut.UpdateAsync(progress);

        adapter.DownloadCalls.Should().Be(1);
        adapter.ApplyCalls.Should().Be(1);
        sut.Status.Should().Be(UpdateStatus.Installing);
        sut.DownloadProgress.Should().Be(100);
        reportedProgress.Should().Equal(10, 60, 100);
        settings.Version.Should().Be("2.1.0");
        settings.ReleaseNotes["2.1.0"].Should().Be("## Notes");
        settings.ReleaseNotesShown["2.1.0"].Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WhenDownloadFails_ExposesErrorAndDoesNotApply()
    {
        var settings = new FakeSettingsService();
        var adapter = new FakeUpdateManagerAdapter
        {
            NextUpdate = new AvailableUpdate(new object(), "2.1.0", "## Notes"),
            DownloadException = new InvalidOperationException("disk full")
        };
        var sut = CreateUpdater(adapter, settings);

        await sut.CheckForUpdatesAsync();
        await sut.UpdateAsync();

        sut.Status.Should().Be(UpdateStatus.Error);
        sut.ErrorMessage.Should().Be("disk full");
        adapter.ApplyCalls.Should().Be(0);
        settings.Version.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenCalledTwiceConcurrently_RunsSingleDownload()
    {
        var adapter = new FakeUpdateManagerAdapter
        {
            NextUpdate = new AvailableUpdate(new object(), "2.1.0", "## Notes"),
            ProgressValues = [100],
            DownloadDelay = TimeSpan.FromMilliseconds(50)
        };
        var sut = CreateUpdater(adapter);

        await sut.CheckForUpdatesAsync();
        await Task.WhenAll(sut.UpdateAsync(), sut.UpdateAsync());

        adapter.DownloadCalls.Should().Be(1);
        adapter.ApplyCalls.Should().Be(1);
    }

    [Fact]
    public async Task MockUpdater_PublishesInstallsAndRestartsIntoTargetVersion()
    {
        var settings = new FakeSettingsService();
        var sut = new MockUpdater(settings)
        {
            MockCurrentVersion = "v0.2.1",
            MockUpdateVersion = "v0.2.2",
            MockReleaseNotes = "## Notes mock"
        };

        sut.CurrentVersion.Should().Be("0.2.1");

        await sut.PublishMockUpdateAsync();
        await sut.UpdateAsync();
        await sut.SimulateRestartAsync();

        sut.CurrentVersion.Should().Be("0.2.2");
        sut.CurrentReleaseNotes.Should().Be("## Notes mock");
        sut.Status.Should().Be(UpdateStatus.Idle);
        sut.UpdateInformations.Should().BeNull();
        settings.ReleaseNotes["0.2.2"].Should().Be("## Notes mock");
        settings.ReleaseNotesShown["0.2.2"].Should().BeFalse();
    }

    [Fact]
    public async Task CurrentReleaseNotes_AreShownOncePerVersion()
    {
        var settings = new FakeSettingsService();
        settings.ReleaseNotes["1.2.3"] = "current notes";
        var sut = CreateUpdater(new FakeUpdateManagerAdapter(), settings, currentVersion: "1.2.3");

        var shouldShow = await sut.ShouldShowCurrentReleaseNotesAsync();
        await sut.MarkCurrentReleaseNotesAsShownAsync();
        var shouldShowAgain = await sut.ShouldShowCurrentReleaseNotesAsync();

        shouldShow.Should().BeTrue();
        shouldShowAgain.Should().BeFalse();
        sut.CurrentReleaseNotes.Should().Be("current notes");
    }

    private static GithubUpdater CreateUpdater(
        FakeUpdateManagerAdapter adapter,
        FakeSettingsService? settings = null,
        string currentVersion = "1.0.0") =>
        new(settings ?? new FakeSettingsService(), adapter, currentVersion);

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
