// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Refresh;
using Looma.Domain.Search;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Sections.Patterns;
using Looma.Presentation.ViewModels.Sections.Projects;
using Looma.Presentation.Tests.TestSupport;

namespace Looma.Presentation.Tests.Sections.Projects;

public sealed class ProjectsViewModelTests
{
    [Fact]
    public void ProjectsList_Loads_Filters_Searches_And_Navigates_To_Add_Form()
    {
        var nav = new FakeNavigationService();
        var projectService = new FakeProjectService
        {
            GetAllResult = ResultT<IReadOnlyList<Project>>.Ok([
                TestData.Project(id: 1, name: "Cardigan", status: Status.InProgress),
                TestData.Project(id: 2, name: "Blanket", status: Status.Finished)
            ])
        };
        var vm = new ProjectsListViewModel(nav, projectService, new FakeNotificationService(), new FakeRefreshService());

        vm.OnNavigatedTo();

        vm.Title.Should().Be("Projets");
        vm.SelectedStatusFilter.Should().Be(vm.StatusFilters[0]);
        vm.CurrentPageEntities.Should().HaveCount(2);

        vm.SelectedStatusFilter = vm.StatusFilters.Single(f => f.Type == Status.Finished);
        vm.CurrentPageEntities.Should().ContainSingle(s => s.Project.Name == "Blanket");

        vm.SearchQuery = "blanket";
        vm.CurrentPageEntities.Should().ContainSingle(s => s.Project.ProjectId == 2);

        vm.OpenAddFormCommand.Execute(null);
        nav.NavigatedTypes.Should().Contain(typeof(ProjectsFormViewModel));
    }

    [Fact]
    public void ProjectsList_Load_Failure_Clears_List_And_Notifies()
    {
        var notifications = new FakeNotificationService();
        var vm = new ProjectsListViewModel(
            new FakeNavigationService(),
            new FakeProjectService { GetAllResult = ResultT<IReadOnlyList<Project>>.Failure("project load failed") },
            notifications,
            new FakeRefreshService());

        vm.OnNavigatedTo();

        vm.CurrentPageEntities.Should().BeEmpty();
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Error && c.Message == "project load failed");
    }

    [Fact]
    public async Task ProjectsForm_InitCreate_Loads_Choices_Selects_Items_And_Saves_With_Images()
    {
        var pattern = TestData.Pattern(id: 3, name: "Pattern");
        var wool = TestData.Wool(id: 4, name: "Merino");
        var nav = new FakeNavigationService();
        var projectService = new FakeProjectService { AddResult = ResultT<Project>.Ok(TestData.Project(id: 10)) };
        var patternService = new FakePatternService { GetAllResult = ResultT<IReadOnlyList<Pattern>>.Ok([pattern]) };
        var woolService = new FakeWoolService { GetAllResult = ResultT<IReadOnlyList<Wool>>.Ok([wool]) };
        var documentService = new FakeDocumentService { AddAllResult = ResultT<IReadOnlyList<Document>>.Ok([]) };
        var picker = new FakeDocumentFilePicker { NextPicks = ["/tmp/project.png"] };
        var vm = CreateFormViewModel(nav, projectService, patternService, woolService, documentService, picker);

        vm.InitCreate();
        await TestHelpers.WaitUntilAsync(() => !vm.IsBusy);

        vm.PatternResults.Should().ContainSingle(p => p.Pattern.Id == pattern.Id);
        vm.WoolResults.Should().ContainSingle(w => w.Wool.Id == wool.Id);

        vm.PatternResults.Single().SelectCommand.Execute(null);
        vm.WoolResults.Single().ToggleCommand.Execute(null);
        await vm.BrowseImagesCommand.ExecuteAsync();
        vm.Name = "Project";

        await vm.SaveCommand.ExecuteAsync();

        projectService.AddRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Name = "Project",
            Status = Status.InProgress,
            PatternId = 3,
            WoolIds = new[] { 4 }
        });
        documentService.AddAllRequests.Should().ContainSingle()
            .Which.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { SourcePath = "/tmp/project.png", Nickname = "project", ProjectId = 10 });
        nav.GoBackCount.Should().Be(1);
    }

    [Fact]
    public async Task ProjectsForm_BrowseImages_Rejects_Non_Image_File()
    {
        var notifications = new FakeNotificationService();
        var vm = CreateFormViewModel(notifications: notifications, picker: new FakeDocumentFilePicker { NextPicks = ["/tmp/readme.txt"] });
        vm.InitCreate();
        await TestHelpers.WaitUntilAsync(() => !vm.IsBusy);

        await vm.BrowseImagesCommand.ExecuteAsync();

        vm.NewImages.Should().BeEmpty();
        vm.ErrorMessage.Should().Be("Seuls les image sont acceptés.");
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Error);
    }

    [Fact]
    public async Task ProjectsForm_InitEdit_Removes_Existing_Image_And_Syncs_Delete_On_Save()
    {
        var image = TestData.Document(storagePath: "/tmp/a.png", type: "png");
        var project = TestData.Project(id: 2, files: [image]);
        var projectService = new FakeProjectService { UpdateResult = ResultT<Project>.Ok(project) };
        var documentService = new FakeDocumentService();
        var vm = CreateFormViewModel(projectService: projectService, documentService: documentService);

        vm.InitEdit(project);
        await TestHelpers.WaitUntilAsync(() => !vm.IsBusy);
        vm.ExistingImages.Single().RemoveCommand!.Execute(null);
        vm.Name = "Updated";

        await vm.SaveCommand.ExecuteAsync();

        documentService.DeleteIds.Should().ContainSingle().Which.Should().Be(image.Id);
        projectService.UpdateRequests.Should().ContainSingle().Which.Id.Should().Be(2);
    }

    [Fact]
    public async Task ProjectsDetail_Loads_Derived_Data_Adjusts_Wool_And_Navigates_To_Pattern()
    {
        var nav = new FakeNavigationService();
        var stockService = new FakeWoolStockService();
        var pattern = TestData.Pattern(id: 7);
        var usage = TestData.WoolUsage(TestData.Wool(id: 8), stockUsed: 1000);
        var vm = CreateDetailViewModel(nav: nav, stockService: stockService);
        vm.Load(TestData.Project(id: 2, pattern: pattern, wools: [usage]));
        vm.WoolAdjustmentQuantityText = "2";
        vm.WoolAdjustmentMode = StockAdjustmentMode.ByBall;
        vm.DeductWoolImmediately = true;

        await vm.Wools.Single().AddCommand.ExecuteAsync();
        vm.OpenPatternCommand.Execute(null);

        stockService.AdjustRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            ProjectId = 2,
            WoolId = 8,
            Mode = StockAdjustmentMode.ByBall,
            IsAddition = true,
            Quantity = 2d,
            DeductImmediately = true
        });
        vm.WoolAdjustmentQuantityText.Should().BeEmpty();
        vm.WoolAdjustmentQuantity.Should().BeNull();
        nav.NavigatedTypes.Should().Contain(typeof(PatternsDetailViewModel));
    }

    [Fact]
    public async Task ProjectsDetail_StartProject_Sends_Status_Update_With_Today_As_BeginDate()
    {
        var projectService = new FakeProjectService();
        projectService.UpdateResult = ResultT<Project>.Ok(TestData.Project(id: 5, status: Status.InProgress));
        var vm = CreateDetailViewModel(projectService: projectService);
        vm.Load(TestData.Project(id: 5, status: Status.Wishlist));

        await vm.StartProjectCommand.ExecuteAsync();

        projectService.UpdateRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = 5,
            Status = Status.InProgress,
            BeginDate = DateOnly.FromDateTime(DateTime.Today)
        });
    }

    [Fact]
    public async Task ProjectsDetail_SaveNote_Updates_Project_Note()
    {
        var projectService = new FakeProjectService
        {
            UpdateResult = ResultT<Project>.Ok(TestData.Project(id: 5, note: "Nouvelle note"))
        };
        var vm = CreateDetailViewModel(projectService: projectService);
        vm.Load(TestData.Project(id: 5, name: "Pull", note: "Ancienne note"));
        vm.Note = "Nouvelle note";

        await vm.SaveNoteCommand.ExecuteAsync();

        projectService.UpdateRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = 5,
            Name = "Pull",
            Note = "Nouvelle note"
        });
        vm.Note.Should().Be("Nouvelle note");
    }

    [Fact]
    public async Task ProjectsDetail_AdjustWool_Without_Positive_Quantity_Notifies_And_Does_Not_Call_Service()
    {
        var notifications = new FakeNotificationService();
        var stockService = new FakeWoolStockService();
        var vm = CreateDetailViewModel(notifications: notifications, stockService: stockService);
        vm.Load(TestData.Project(wools: [TestData.WoolUsage()]));

        await vm.Wools.Single().AddCommand.ExecuteAsync();

        stockService.AdjustRequests.Should().BeEmpty();
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Error && c.Message == "Indiquez une quantité supérieure à zéro.");
    }

    [Fact]
    public void ProjectsDetail_AdjustWool_Empty_Quantity_Text_Clears_Parsed_Quantity()
    {
        var vm = CreateDetailViewModel();

        vm.WoolAdjustmentQuantityText = "2";
        vm.WoolAdjustmentQuantityText = string.Empty;

        vm.WoolAdjustmentQuantity.Should().BeNull();
        vm.CanAdjustWool.Should().BeFalse();
    }

    [Fact]
    public async Task ProjectsDetail_Delete_Success_Goes_Back()
    {
        var nav = new FakeNavigationService();
        var projectService = new FakeProjectService();
        var vm = CreateDetailViewModel(nav: nav, projectService: projectService);
        vm.Load(TestData.Project(id: 12));

        await vm.ConfirmDeleteCommand.ExecuteAsync();

        projectService.DeleteIds.Should().ContainSingle().Which.Should().Be(12);
        nav.GoBackCount.Should().Be(1);
    }

    [Fact]
    public void ProjectsDetail_Image_Navigation_Wraps_Around()
    {
        var vm = CreateDetailViewModel();
        vm.Load(TestData.Project(files: [
            TestData.Document(storagePath: "/tmp/1.png"),
            TestData.Document(storagePath: "/tmp/2.jpg")
        ]));

        vm.SelectedImageIndex.Should().Be(0);
        vm.PreviousImageCommand.Execute(null);
        vm.SelectedImageIndex.Should().Be(1);
        vm.NextImageCommand.Execute(null);
        vm.SelectedImageIndex.Should().Be(0);
        vm.ImagePositionDisplay.Should().Be("1 / 2");
    }

    [Fact]
    public async Task ProjectsFinish_Confirm_Adjusts_Pending_Wool_And_Marks_Project_Finished()
    {
        var nav = new FakeNavigationService();
        var wool = TestData.Wool(id: 4, weight: 50, stock: 3000);
        var project = TestData.Project(
            id: 6,
            wools: [TestData.WoolUsage(wool, stockUsed: 1500, stockAlreadyUsed: 500)]);
        var projectService = new FakeProjectService
        {
            UpdateResult = ResultT<Project>.Ok(project)
        };
        projectService.ByIdResults[6] = ResultT<Project>.Ok(project);
        var stockService = new FakeWoolStockService();
        var vm = new ProjectsFinishViewModel(nav, projectService, stockService, new FakeNotificationService());
        vm.Load(6);
        vm.OnNavigatedTo();
        vm.EndDate = new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero);
        vm.Wools.Single().QuantityToDeduct = 2;

        await vm.ConfirmCommand.ExecuteAsync();

        stockService.AdjustRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            ProjectId = 6,
            WoolId = 4,
            Mode = StockAdjustmentMode.ByBall,
            IsAddition = true,
            Quantity = 1d,
            DeductImmediately = false
        });
        projectService.UpdateRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = 6,
            Status = Status.Finished,
            EndDate = new DateOnly(2026, 6, 16)
        });
        nav.GoBackCount.Should().Be(1);
    }

    [Fact]
    public async Task ProjectsFinish_Rejects_Deduction_When_Stock_Is_Insufficient()
    {
        var notifications = new FakeNotificationService();
        var wool = TestData.Wool(name: "Limited", stock: 500);
        var project = TestData.Project(wools: [TestData.WoolUsage(wool, stockUsed: 2000)]);
        var projectService = new FakeProjectService();
        projectService.ByIdResults[project.ProjectId] = ResultT<Project>.Ok(project);
        var vm = new ProjectsFinishViewModel(new FakeNavigationService(), projectService, new FakeWoolStockService(), notifications);
        vm.Load(project.ProjectId);
        vm.OnNavigatedTo();

        await vm.ConfirmCommand.ExecuteAsync();

        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Error && c.Message == "Le stock disponible est insuffisant pour Limited.");
        projectService.UpdateRequests.Should().BeEmpty();
    }

    private static ProjectsFormViewModel CreateFormViewModel(
        FakeNavigationService? nav = null,
        FakeProjectService? projectService = null,
        FakePatternService? patternService = null,
        FakeWoolService? woolService = null,
        FakeDocumentService? documentService = null,
        FakeDocumentFilePicker? picker = null,
        FakeNotificationService? notifications = null)
        => new(
            nav ?? new FakeNavigationService(),
            projectService ?? new FakeProjectService(),
            patternService ?? new FakePatternService(),
            woolService ?? new FakeWoolService(),
            documentService ?? new FakeDocumentService(),
            picker ?? new FakeDocumentFilePicker(),
            notifications ?? new FakeNotificationService(),
            new PatternSearchSpec(),
            new WoolSearchSpec());

    private static ProjectsDetailViewModel CreateDetailViewModel(
        FakeNavigationService? nav = null,
        FakeProjectService? projectService = null,
        FakeNotificationService? notifications = null,
        FakeWoolStockService? stockService = null,
        FakeDocumentFilePicker? picker = null)
        => new(
            nav ?? new FakeNavigationService(),
            projectService ?? new FakeProjectService(),
            notifications ?? new FakeNotificationService(),
            stockService ?? new FakeWoolStockService(),
            picker ?? new FakeDocumentFilePicker(),
            new FakeRefreshService());
}
