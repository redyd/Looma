// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Presentation.Navigation;
using Looma.Presentation.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Looma.Presentation.Tests.Navigation;

public sealed class NavigationServiceTests
{
    [Fact]
    public void NavigateTo_Trims_Older_Transient_Pages_But_Keeps_Root_Pages()
    {
        var nav = new NavigationService(new ServiceCollection().BuildServiceProvider());
        var root = new RootPage();
        var visited = new List<PageViewModelBase>();
        nav.Navigated += (_, page) => visited.Add(page);

        nav.PushPage(root);
        nav.NavigateTo<TransientPageA>();
        var pageA = visited.OfType<TransientPageA>().Single();
        nav.NavigateTo<TransientPageB>();
        var pageB = visited.OfType<TransientPageB>().Single();
        nav.NavigateTo<TransientPageC>();
        var pageC = visited.OfType<TransientPageC>().Single();

        nav.GoBack();
        nav.CurrentPage.Should().BeOfType<TransientPageB>();
        pageA.DestroyCount.Should().Be(1);
        pageB.DestroyCount.Should().Be(0);
        pageC.DestroyCount.Should().Be(1);

        nav.GoBack();
        nav.CurrentPage.Should().Be(root);
        nav.CanGoBack.Should().BeFalse();
        root.DestroyCount.Should().Be(0);
        pageB.DestroyCount.Should().Be(1);

        visited.Select(page => page.GetType()).Should().Equal(
            typeof(RootPage),
            typeof(TransientPageA),
            typeof(TransientPageB),
            typeof(TransientPageC),
            typeof(TransientPageB),
            typeof(RootPage));
    }

    [Fact]
    public void ClearHistory_Destroys_Current_Page_And_Back_Stack()
    {
        var nav = new NavigationService(new ServiceCollection().BuildServiceProvider());
        var root = new RootPage();
        var visited = new List<PageViewModelBase>();
        nav.Navigated += (_, page) => visited.Add(page);

        nav.PushPage(root);
        nav.NavigateTo<TransientPageA>();

        var pageA = visited.OfType<TransientPageA>().Single();
        nav.ClearHistory();

        root.DestroyCount.Should().Be(1);
        pageA.DestroyCount.Should().Be(1);
        nav.CurrentPage.Should().BeNull();
        nav.CanGoBack.Should().BeFalse();
    }

    public sealed class RootPage : PageViewModelBase
    {
        public override bool KeepAliveInNavigationHistory => true;
        public int DestroyCount { get; private set; }
        protected override void OnDestroy()
        {
            DestroyCount++;
            base.OnDestroy();
        }
    }

    public sealed class TransientPageA : PageViewModelBase
    {
        public TransientPageA(INavigationService nav) => _ = nav;
        public int DestroyCount { get; private set; }
        protected override void OnDestroy()
        {
            DestroyCount++;
            base.OnDestroy();
        }
    }

    public sealed class TransientPageB : PageViewModelBase
    {
        public TransientPageB(INavigationService nav) => _ = nav;
        public int DestroyCount { get; private set; }
        protected override void OnDestroy()
        {
            DestroyCount++;
            base.OnDestroy();
        }
    }

    public sealed class TransientPageC : PageViewModelBase
    {
        public TransientPageC(INavigationService nav) => _ = nav;
        public int DestroyCount { get; private set; }
        protected override void OnDestroy()
        {
            DestroyCount++;
            base.OnDestroy();
        }
    }
}
