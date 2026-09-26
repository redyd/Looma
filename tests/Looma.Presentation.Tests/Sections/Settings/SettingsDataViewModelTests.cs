// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Backup;
using Looma.Domain.Core;
using Looma.Domain.Integrity;
using Looma.Presentation.Notifications;
using Looma.Presentation.Tests.TestSupport;
using Looma.Presentation.ViewModels.Sections.Settings;

namespace Looma.Presentation.Tests.Sections.Settings;

public sealed class SettingsDataViewModelTests
{
    private readonly FakeBackupService _backup = new();
    private readonly FakeDataIntegrityService _integrity = new();
    private readonly FakeBackupFilePicker _picker = new();
    private readonly FakeAppLifetimeService _lifetime = new();
    private readonly FakeNotificationService _notifications = new();

    private SettingsDataViewModel CreateViewModel() =>
        new(_backup, _integrity, _picker, _lifetime, _notifications);

    [Fact]
    public async Task Export_WritesToPickedLocation()
    {
        var vm = CreateViewModel();

        await vm.ExportCommand.ExecuteAsync(null);

        _backup.Exported.Should().Equal("/tmp/export.looma");
        _notifications.Calls.Should().ContainSingle(call => call.Severity == NotificationSeverity.Success);
    }

    [Fact]
    public async Task Export_WhenPickerCancelled_DoesNothing()
    {
        _picker.SavePath = null;
        var vm = CreateViewModel();

        await vm.ExportCommand.ExecuteAsync(null);

        _backup.Exported.Should().BeEmpty();
    }

    [Fact]
    public async Task PickImport_ValidArchive_AsksConfirmationWithoutStaging()
    {
        var vm = CreateViewModel();

        await vm.PickImportCommand.ExecuteAsync(null);

        vm.IsImportConfirmationVisible.Should().BeTrue();
        vm.ImportSummary.Should().NotBeNullOrWhiteSpace();
        _backup.Staged.Should().BeEmpty();
        _lifetime.RestartCalls.Should().Be(0);
    }

    [Fact]
    public async Task PickImport_InvalidArchive_ShowsErrorAndNoConfirmation()
    {
        _backup.ValidateResult = ResultT<BackupManifest>.Failure("bad archive");
        var vm = CreateViewModel();

        await vm.PickImportCommand.ExecuteAsync(null);

        vm.IsImportConfirmationVisible.Should().BeFalse();
        _notifications.Calls.Should().ContainSingle(call =>
            call.Severity == NotificationSeverity.Error && call.Message == "bad archive");
    }

    [Fact]
    public async Task ConfirmImport_StagesArchiveAndRestarts()
    {
        var vm = CreateViewModel();
        await vm.PickImportCommand.ExecuteAsync(null);

        await vm.ConfirmImportCommand.ExecuteAsync(null);

        _backup.Staged.Should().Equal("/tmp/backup.looma");
        _lifetime.RestartCalls.Should().Be(1);
        vm.IsImportConfirmationVisible.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmImport_WhenStagingFails_DoesNotRestart()
    {
        _backup.StageResult = ResultT<BackupManifest>.Failure("disk full");
        var vm = CreateViewModel();
        await vm.PickImportCommand.ExecuteAsync(null);

        await vm.ConfirmImportCommand.ExecuteAsync(null);

        _lifetime.RestartCalls.Should().Be(0);
        _notifications.Calls.Should().Contain(call => call.Severity == NotificationSeverity.Error && call.Message == "disk full");
    }

    [Fact]
    public async Task CancelImport_HidesConfirmation()
    {
        var vm = CreateViewModel();
        await vm.PickImportCommand.ExecuteAsync(null);

        vm.CancelImportCommand.Execute(null);

        vm.IsImportConfirmationVisible.Should().BeFalse();
        _backup.Staged.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckIntegrity_ListsIssuesAndEnablesMatchingRepairs()
    {
        _integrity.Report = new IntegrityReport([
            new IntegrityIssue(IntegrityIssueKind.DocumentFileMissing, "id", "Doc"),
            new IntegrityIssue(IntegrityIssueKind.DocumentFileOrphan, "file.pdf")
        ]);
        var vm = CreateViewModel();

        await vm.CheckIntegrityCommand.ExecuteAsync(null);

        vm.IntegrityIssues.Should().HaveCount(2);
        vm.HasIntegrityIssues.Should().BeTrue();
        vm.HasMissingDocuments.Should().BeTrue();
        vm.HasOrphanFiles.Should().BeTrue();
        vm.QuarantineOrphansCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task QuarantineOrphans_RechecksAfterRepair()
    {
        _integrity.Report = new IntegrityReport([new IntegrityIssue(IntegrityIssueKind.DocumentFileOrphan, "file.pdf")]);
        var vm = CreateViewModel();
        await vm.CheckIntegrityCommand.ExecuteAsync(null);

        await vm.QuarantineOrphansCommand.ExecuteAsync(null);

        _integrity.QuarantineCalls.Should().Be(1);
        vm.IsHealthy.Should().BeTrue();
        vm.HasOrphanFiles.Should().BeFalse();
    }
}
