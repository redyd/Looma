// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Globalization;
using Looma.Domain.Core;
using Looma.Domain.Logging;
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

        await vm.Updater.CheckForUpdatesCommand.ExecuteAsync(null);

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

        await vm.Updater.CheckForUpdatesCommand.ExecuteAsync(null);

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

        await vm.Updater.CheckForUpdatesCommand.ExecuteAsync(null);

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

        await vm.Updater.CheckForUpdatesCommand.ExecuteAsync(null);

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

        await vm.Updater.CheckForUpdatesCommand.ExecuteAsync(null);

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

        vm.Updater.ShowCurrentReleaseNotesCommand.Execute(null);

        interaction.CurrentReleaseNotesRequestCount.Should().Be(1);
    }

    [Fact]
    public void SelectedLanguage_WhenChanged_UpdatesCurrentCultureAndNotifies()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        var originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        var originalDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

        try
        {
            var notifications = new FakeNotificationService();
            var translation = new TranslationService();
            translation.SetCulture("fr");

            var vm = CreateViewModel(
                notifications,
                new FakeUpdaterService(),
                new FakeUpdateInteractionService(),
                translation: translation);

            vm.OnNavigatedTo();
            notifications.Calls.Clear();

            vm.SelectedLanguage = vm.Languages.Single(language => language.Culture == "en");

            CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Should().Be("en");
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Should().Be("en");
            CultureInfo.DefaultThreadCurrentCulture?.TwoLetterISOLanguageName.Should().Be("en");
            CultureInfo.DefaultThreadCurrentUICulture?.TwoLetterISOLanguageName.Should().Be("en");
            notifications.Calls.Should().ContainSingle(call =>
                call.Severity == NotificationSeverity.Success
                && call.Message == "Language changed.");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUiCulture;
        }
    }

    [Fact]
    public void OnNavigatedTo_WhenSelectedThemeJsonIsInvalid_ShowsClearNotification()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "looma-settings-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        var themePath = Path.Combine(tempFolder, "bad-theme.json");
        File.WriteAllText(themePath, """{"Name":""");

        try
        {
            var notifications = new FakeNotificationService();
            var updater = new FakeUpdaterService();
            var interaction = new FakeUpdateInteractionService();
            var storage = new FakeThemeStorage
            {
                ThemeFiles = [themePath],
                SelectedThemePath = themePath
            };
            var vm = CreateViewModel(notifications, updater, interaction, storage);

            vm.OnNavigatedTo();

            notifications.Calls.Should().ContainSingle(call =>
                call.Severity == NotificationSeverity.Error
                && call.Message.Contains("Impossible de charger les thèmes : Le fichier de thème \"bad-theme.json\" contient un JSON invalide.")
                && call.Message.Contains("Vérifiez la syntaxe du fichier."));
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    private static SettingsViewModel CreateViewModel(
        FakeNotificationService notifications,
        FakeUpdaterService updater,
        FakeUpdateInteractionService interaction,
        FakeThemeStorage? themeStorage = null,
        TranslationService? translation = null) =>
        new(
            new ThemeService(),
            themeStorage ?? new FakeThemeStorage(),
            new FakeThemeFilePicker(),
            notifications,
            translation ?? new TranslationService(),
            new SettingsUpdaterViewModel(notifications, updater, interaction),
            NullDomainLogger.Instance);
}
