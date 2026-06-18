// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Presentation.ViewModels.Base;
using Looma.Presentation.ViewModels.Main;
using Looma.Presentation.Tests.TestSupport;

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
        var settings = CreateSection();

        var vm = new MainViewModel(projects, stocks, patterns, documents, settings, notifications);

        vm.ProjectsSection.Should().Be(projects);
        vm.StocksSection.Should().Be(stocks);
        vm.PatternsSection.Should().Be(patterns);
        vm.DocumentsSection.Should().Be(documents);
        vm.SettingsSection.Should().Be(settings);
        vm.Notifications.Should().Be(notifications);
        vm.SelectedTabIndex.Should().Be(0);
    }

    private static SectionNavigationViewModel CreateSection() =>
        new(new FakeNavigationService(), new DummyPageViewModel());

    private sealed class DummyPageViewModel : PageViewModelBase;
}
