// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.Tests.TestSupport;
using Looma.Presentation.ViewModels.Sections.Settings;

namespace Looma.Presentation.Tests.Sections.Settings;

public sealed class SettingsViewModelTests
{
    [Fact]
    public async Task CheckForUpdates_WhenNoUpdate_ShowsUpToDateNotification()
    {
        var notifications = new FakeNotificationService();
        var updater = new FakeUpdaterService
        {
            OnCheck = (service, _) => service.Status = UpdateStatus.Idle
        };
        var interaction = new FakeUpdateInteractionService();
        var vm = CreateViewModel(notifications, updater, interaction);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        updater.CheckCalls.Should().Be(1);
        interaction.UpdatePromptRequestCount.Should().Be(0);
        notifications.Calls.Should().ContainSingle(call =>
            call.Severity == NotificationSeverity.Info
            && call.Message == "Looma est à jour.");
    }

    [Fact]
    public async Task CheckForUpdates_WhenUpdateAlreadyAvailable_RequestsReusablePrompt()
    {
        var notifications = new FakeNotificationService();
        var updater = new FakeUpdaterService
        {
            Status = UpdateStatus.Available,
            UpdateInformations = new UpdateInformations { Version = "2.0.0", ReleaseNotes = "notes" }
        };
        var interaction = new FakeUpdateInteractionService();
        var vm = CreateViewModel(notifications, updater, interaction);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        updater.CheckCalls.Should().Be(0);
        interaction.UpdatePromptRequestCount.Should().Be(1);
        notifications.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckForUpdates_WhenManualCheckFindsUpdate_RequestsReusablePrompt()
    {
        var notifications = new FakeNotificationService();
        var updater = new FakeUpdaterService
        {
            OnCheck = (service, _) =>
            {
                service.Status = UpdateStatus.Available;
                service.UpdateInformations = new UpdateInformations { Version = "2.0.0", ReleaseNotes = "notes" };
            }
        };
        var interaction = new FakeUpdateInteractionService();
        var vm = CreateViewModel(notifications, updater, interaction);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        updater.CheckCalls.Should().Be(1);
        interaction.UpdatePromptRequestCount.Should().Be(1);
        notifications.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckForUpdates_WhenUpdaterFails_ShowsErrorNotification()
    {
        var notifications = new FakeNotificationService();
        var updater = new FakeUpdaterService
        {
            OnCheck = (service, _) =>
            {
                service.Status = UpdateStatus.Error;
                service.ErrorMessage = "network down";
            }
        };
        var interaction = new FakeUpdateInteractionService();
        var vm = CreateViewModel(notifications, updater, interaction);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        interaction.UpdatePromptRequestCount.Should().Be(0);
        notifications.Calls.Should().ContainSingle(call =>
            call.Severity == NotificationSeverity.Error
            && call.Message == "network down");
    }

    [Fact]
    public async Task CheckForUpdates_WhenAlreadyChecking_DoesNothing()
    {
        var notifications = new FakeNotificationService();
        var updater = new FakeUpdaterService { Status = UpdateStatus.Checking };
        var interaction = new FakeUpdateInteractionService();
        var vm = CreateViewModel(notifications, updater, interaction);

        await vm.CheckForUpdatesCommand.ExecuteAsync(null);

        updater.CheckCalls.Should().Be(0);
        interaction.UpdatePromptRequestCount.Should().Be(0);
        notifications.Calls.Should().BeEmpty();
    }

    [Fact]
    public void ShowCurrentReleaseNotes_RequestsReleaseNotesScreen()
    {
        var notifications = new FakeNotificationService();
        var updater = new FakeUpdaterService();
        var interaction = new FakeUpdateInteractionService();
        var vm = CreateViewModel(notifications, updater, interaction);

        vm.ShowCurrentReleaseNotesCommand.Execute(null);

        interaction.CurrentReleaseNotesRequestCount.Should().Be(1);
    }

    private static SettingsViewModel CreateViewModel(
        FakeNotificationService notifications,
        FakeUpdaterService updater,
        FakeUpdateInteractionService interaction) =>
        new(
            new ThemeService(),
            new FakeThemeStorage(),
            new FakeThemeFilePicker(),
            notifications,
            updater,
            interaction);
}
