// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Main;
using Looma.Presentation.Tests.TestSupport;
using Looma.Domain.Core;

namespace Looma.Presentation.Tests.Main;

public sealed class MainViewModelTests
{
    [Fact]
    public void SectionNavigation_Pushes_Initial_Page_And_GoBack_Delegates_To_Navigation()
    {
        var nav = new FakeNavigationService { CanGoBack = true };
        var initialPage = new DummyPageViewModel();

        var vm = new SectionNavigationViewModel(nav, initialPage);

        vm.CurrentPage.Should().Be(initialPage);
        vm.CanGoBack.Should().BeTrue();
        vm.GoBackCommand.CanExecute(null).Should().BeTrue();

        vm.GoBackCommand.Execute(null);

        nav.PushedPages.Should().ContainSingle().Which.Should().Be(initialPage);
        nav.GoBackCount.Should().Be(1);
    }

    [Fact]
    public void MainViewModel_Stores_Sections_Notifications_And_Defaults_To_First_Tab()
    {
        var notifications = new FakeNotificationService();
        var projects = CreateSection();
        var stocks = CreateSection();
        var patterns = CreateSection();
        var documents = CreateSection();
        var statistics = CreateSection();
        var settings = CreateSection();

        var updater = new FakeUpdaterService();
        var updateInteraction = new FakeUpdateInteractionService();

        var vm = new MainViewModel(projects, stocks, patterns, documents, statistics, settings, notifications, updater, updateInteraction);

        vm.ProjectsSection.Should().Be(projects);
        vm.StocksSection.Should().Be(stocks);
        vm.PatternsSection.Should().Be(patterns);
        vm.DocumentsSection.Should().Be(documents);
        vm.StatisticsSection.Should().Be(statistics);
        vm.SettingsSection.Should().Be(settings);
        vm.Notifications.Should().Be(notifications);
        vm.SelectedTabIndex.Should().Be(0);
    }

    [Fact]
    public void UpdatePromptRequest_WhenUpdateExists_ShowsReusablePrompt()
    {
        var updater = new FakeUpdaterService
        {
            UpdateInformations = new UpdateInformations
            {
                Version = "2.0.0",
                ReleaseNotes = "## Notes"
            }
        };
        var interaction = new FakeUpdateInteractionService();
        var vm = CreateMainViewModel(updater, interaction);

        interaction.RequestUpdatePrompt();

        vm.IsUpdatePromptVisible.Should().BeTrue();
        vm.UpdateVersion.Should().Be("2.0.0");
        vm.UpdateReleaseNotes.Should().Be("## Notes");
    }

    [Fact]
    public async Task ConfirmUpdate_CallsUpdaterAndLocksPromptWhileRunning()
    {
        var updater = new FakeUpdaterService
        {
            UpdateInformations = new UpdateInformations
            {
                Version = "2.0.0",
                ReleaseNotes = "notes"
            },
            OnUpdate = service =>
            {
                service.Status = UpdateStatus.Installing;
                service.DownloadProgress = 100;
            }
        };
        var interaction = new FakeUpdateInteractionService();
        var vm = CreateMainViewModel(updater, interaction);
        interaction.RequestUpdatePrompt();

        await vm.ConfirmUpdateCommand.ExecuteAsync(null);

        updater.UpdateCalls.Should().Be(1);
        vm.CanConfirmUpdate.Should().BeFalse();
        vm.CanCloseUpdatePrompt.Should().BeFalse();
        vm.DownloadProgress.Should().Be(100);
    }

    [Fact]
    public async Task CloseReleaseNotes_MarksCurrentVersionAsShown()
    {
        var updater = new FakeUpdaterService { CurrentReleaseNotes = "notes" };
        var interaction = new FakeUpdateInteractionService();
        var vm = CreateMainViewModel(updater, interaction);

        interaction.RequestCurrentReleaseNotes();
        vm.IsReleaseNotesVisible.Should().BeTrue();

        await vm.CloseReleaseNotesCommand.ExecuteAsync(null);

        vm.IsReleaseNotesVisible.Should().BeFalse();
        updater.MarkShownCalls.Should().Be(1);
    }

    private static SectionNavigationViewModel CreateSection() =>
        new(new FakeNavigationService(), new DummyPageViewModel());

    private static MainViewModel CreateMainViewModel(
        FakeUpdaterService updater,
        FakeUpdateInteractionService interaction) =>
        new(
            CreateSection(),
            CreateSection(),
            CreateSection(),
            CreateSection(),
            CreateSection(),
            CreateSection(),
            new FakeNotificationService(),
            updater,
            interaction);

    private sealed class DummyPageViewModel : PageViewModelBase;
}
