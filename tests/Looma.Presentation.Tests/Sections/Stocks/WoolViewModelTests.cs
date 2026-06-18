// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia.Media;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Refresh;
using Looma.Domain.Search;
using Looma.Domain.Services;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Sections.Stocks;
using Looma.Presentation.Tests.TestSupport;

namespace Looma.Presentation.Tests.Sections.Stocks;

public sealed class WoolViewModelTests
{
    [Fact]
    public void WoolForm_InitCreate_Resets_State_And_Disables_Save_Until_Required_Data_Is_Valid()
    {
        var vm = CreateFormViewModel();

        vm.InitCreate();

        vm.Title.Should().Be("Nouvelle laine");
        vm.SelectedColorHex.Should().Be("#808080");
        vm.SaveCommand.CanExecute(null).Should().BeFalse();

        FillValidWoolForm(vm);

        vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task WoolForm_Save_Create_Sends_Request_Notifies_And_Goes_Back()
    {
        var nav = new FakeNavigationService();
        var woolService = new FakeWoolService { AddResult = ResultT<Wool>.Ok(TestData.Wool(id: 42)) };
        var notifications = new FakeNotificationService();
        var vm = CreateFormViewModel(nav, woolService, notifications);
        vm.InitCreate();
        FillValidWoolForm(vm);
        vm.SelectedColor = Color.FromRgb(0x12, 0x34, 0x56);
        vm.AddColorCommand.Execute(null);

        await vm.SaveCommand.ExecuteAsync();

        woolService.AddRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Name = "Sock", Brand = "Drops", Material = "Wool", Colors = new[] { "#123456" }, Weight = 50d, Length = 170d, Stock = 1000d, NeedleMinSize = 3.25d, NeedleMaxSize = 3.75d });
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Success && c.Message == "La laine a été créée.");
        nav.GoBackCount.Should().Be(1);
        vm.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task WoolForm_Save_Edit_Sends_Update_Request()
    {
        var woolService = new FakeWoolService { UpdateResult = ResultT<Wool>.Ok(TestData.Wool(id: 8)) };
        var vm = CreateFormViewModel(woolService: woolService);
        vm.InitEdit(TestData.Wool(id: 8));
        FillValidWoolForm(vm);

        await vm.SaveCommand.ExecuteAsync();

        woolService.UpdateRequests.Should().ContainSingle().Which.Id.Should().Be(8);
        woolService.AddRequests.Should().BeEmpty();
    }

    [Fact]
    public void WoolForm_InitEdit_Selects_Existing_Needle_Range_Item()
    {
        var vm = CreateFormViewModel();

        vm.InitEdit(TestData.Wool(needleMin: 3.25, needleMax: 3.75));

        vm.SelectedNeedleRange.Should().BeSameAs(vm.NeedleRanges.Single(r => r.NeedleRange.Type == WoolType.Fine));
    }

    [Theory]
    [InlineData("-1", "50")]
    [InlineData("10", "0")]
    public void WoolForm_SaveCommand_Is_Disabled_For_Invalid_Numeric_Data(
        string weight,
        string length)
    {
        var vm = CreateFormViewModel();
        vm.InitCreate();
        FillValidWoolForm(vm);
        vm.Weight = weight;
        vm.Length = length;

        vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void WoolForm_SaveCommand_Is_Disabled_When_No_Needle_Range_Is_Selected()
    {
        var vm = CreateFormViewModel();
        vm.InitCreate();
        FillValidWoolForm(vm);
        vm.SelectedNeedleRange = null;

        vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void WoolList_OnNavigatedTo_Loads_Paginates_Searches_And_Registers_Refresh()
    {
        var refresh = new FakeRefreshService();
        var woolService = new FakeWoolService
        {
            GetAllResult = ResultT<IReadOnlyList<Wool>>.Ok(Enumerable.Range(1, 13)
                .Select(i => TestData.Wool(i, name: i == 13 ? "Merino Red" : $"Wool {i}", brand: "Brand"))
                .ToList())
        };
        var vm = new WoolListViewModel(
            new FakeNavigationService(),
            woolService,
            new FakeNotificationService(),
            new WoolSearchSpec(),
            refresh);

        vm.OnNavigatedTo();

        vm.Title.Should().BeEmpty();
        vm.CurrentPageEntities.Should().HaveCount(12);
        vm.TotalPages.Should().Be(2);
        vm.HasNextPage.Should().BeTrue();
        refresh.SubscriberCount.Should().Be(1);

        vm.NextPageCommand.Execute(null);
        vm.CurrentPage.Should().Be(2);
        vm.CurrentPageEntities.Should().ContainSingle();

        vm.SearchQuery = "red";
        vm.CurrentPage.Should().Be(1);
        vm.CurrentPageEntities.Should().ContainSingle(s => s.Wool.Name == "Merino Red");

        refresh.RequestRefresh(RefreshScope.Wools, "test");
        woolService.GetAllCalls.Should().Be(2);

        vm.OnNavigatedFrom();
        refresh.SubscriberCount.Should().Be(0);
    }

    [Fact]
    public void WoolList_Load_Failure_Clears_List_And_Notifies()
    {
        var notifications = new FakeNotificationService();
        var vm = new WoolListViewModel(
            new FakeNavigationService(),
            new FakeWoolService { GetAllResult = ResultT<IReadOnlyList<Wool>>.Failure("boom") },
            notifications,
            new WoolSearchSpec(),
            new FakeRefreshService());

        vm.OnNavigatedTo();

        vm.CurrentPageEntities.Should().BeEmpty();
        vm.IsListEmpty.Should().BeTrue();
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Error && c.Message == "boom");
    }

    [Fact]
    public async Task WoolDetail_AdjustStock_Converts_Weight_To_Stock_And_Notifies()
    {
        var woolService = new FakeWoolService();
        var notifications = new FakeNotificationService();
        var vm = CreateDetailViewModel(woolService: woolService, notifications: notifications);
        vm.Load(TestData.Wool(id: 5, weight: 50, stock: 1000));
        vm.AdjustmentMode = StockAdjustmentMode.ByWeight;
        vm.AdjustQuantity = 25;

        await vm.AdjustStockCommand.ExecuteAsync(true);

        woolService.AddStockRequests.Should().ContainSingle().Which.Should().Be((5, 500d));
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Success);
    }

    [Fact]
    public void WoolDetail_Load_Sets_Single_Type_Image_From_Needle_Range()
    {
        var vm = CreateDetailViewModel();

        vm.Load(TestData.Wool(needleMin: 3.25, needleMax: 3.75));

        vm.Image.Should().Be("avares://Looma.App/Assets/WoolTypeImages/fine.png");
    }

    [Fact]
    public async Task WoolDetail_Delete_Success_Goes_Back()
    {
        var nav = new FakeNavigationService();
        var vm = CreateDetailViewModel(nav);
        vm.Load(TestData.Wool(id: 3));

        await vm.ConfirmDeleteCommand.ExecuteAsync();

        nav.GoBackCount.Should().Be(1);
    }

    [Fact]
    public void WoolDetail_Edit_Should_Use_Refreshed_Wool_After_OnNavigatedTo_Bug()
    {
        var nav = new FakeNavigationService();
        var oldWool = TestData.Wool(id: 7, name: "Old");
        var refreshedWool = TestData.Wool(id: 7, name: "Fresh");
        var woolService = new FakeWoolService();
        woolService.ByIdResults[7] = ResultT<Wool>.Ok(refreshedWool);
        var editVm = CreateFormViewModel();
        nav.ViewModelFactory = type => editVm;
        var vm = CreateDetailViewModel(nav, woolService);
        vm.Load(oldWool);

        vm.OnNavigatedTo();
        vm.EditCommand.Execute(null);

        editVm.Name.Should().Be("Fresh", "editing after a refresh should use the latest wool, not the stale object loaded for navigation");
    }

    private static WoolFormViewModel CreateFormViewModel(
        FakeNavigationService? nav = null,
        FakeWoolService? woolService = null,
        FakeNotificationService? notifications = null)
        => new(nav ?? new FakeNavigationService(), woolService ?? new FakeWoolService(), notifications ?? new FakeNotificationService());

    private static WoolDetailViewModel CreateDetailViewModel(
        FakeNavigationService? nav = null,
        FakeWoolService? woolService = null,
        FakeNotificationService? notifications = null,
        FakeRefreshService? refresh = null)
        => new(
            nav ?? new FakeNavigationService(),
            woolService ?? new FakeWoolService(),
            notifications ?? new FakeNotificationService(),
            new WoolStockCalculator(),
            refresh ?? new FakeRefreshService());

    private static void FillValidWoolForm(WoolFormViewModel vm)
    {
        vm.Name = "Sock";
        vm.Brand = "Drops";
        vm.Material = "Wool";
        vm.Weight = "50";
        vm.Length = "170";
        vm.SelectedNeedleRange = vm.NeedleRanges.Single(r => r.NeedleRange.Type == WoolType.Fine);
    }
}
