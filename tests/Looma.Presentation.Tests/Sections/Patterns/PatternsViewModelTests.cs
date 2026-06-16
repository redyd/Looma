// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Refresh;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Sections.Patterns;
using Looma.Presentation.ViewModels.Sections.Projects;
using Looma.Presentation.ViewModels.Shared.Documents;
using Looma.Presentation.Tests.TestSupport;

namespace Looma.Presentation.Tests.Sections.Patterns;

public sealed class PatternsViewModelTests
{
    [Fact]
    public void PatternsList_Loads_Searches_And_Navigates_To_Add_Form()
    {
        var nav = new FakeNavigationService();
        var patternService = new FakePatternService
        {
            GetAllResult = ResultT<IReadOnlyList<Pattern>>.Ok([
                TestData.Pattern(id: 1, name: "Cardigan", type: PatternType.Crochet),
                TestData.Pattern(id: 2, name: "Socks", type: PatternType.Tricot)
            ])
        };
        var vm = new PatternsListViewModel(nav, patternService, new FakeNotificationService(), new FakeRefreshService());

        vm.OnNavigatedTo();

        vm.Title.Should().Be("Mes patrons");
        vm.CurrentPageEntities.Should().HaveCount(2);
        vm.SearchQuery = "tricot";
        vm.CurrentPageEntities.Should().ContainSingle(s => s.Pattern.Name == "Socks");

        vm.OpenAddFormCommand.Execute(null);

        nav.NavigatedTypes.Should().Contain(typeof(PatternsFormViewModel));
    }

    [Fact]
    public void PatternsList_Load_Failure_Clears_List_And_Notifies()
    {
        var notifications = new FakeNotificationService();
        var vm = new PatternsListViewModel(
            new FakeNavigationService(),
            new FakePatternService { GetAllResult = ResultT<IReadOnlyList<Pattern>>.Failure("patterns unavailable") },
            notifications,
            new FakeRefreshService());

        vm.OnNavigatedTo();

        vm.CurrentPageEntities.Should().BeEmpty();
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Error && c.Message == "patterns unavailable");
    }

    [Fact]
    public async Task PatternsForm_Create_Saves_Pattern_Then_Attaches_New_Documents()
    {
        var nav = new FakeNavigationService();
        var patternService = new FakePatternService { AddResult = ResultT<Pattern>.Ok(TestData.Pattern(id: 12)) };
        var documentService = new FakeDocumentService { AddAllResult = ResultT<IReadOnlyList<Document>>.Ok([]) };
        var notifications = new FakeNotificationService();
        var vm = CreateFormViewModel(nav, patternService, documentService, notifications);
        vm.InitCreate();
        vm.Name = "Summer top";
        vm.Url = "https://example.test";
        vm.Type = PatternType.Tricot;
        vm.Documents.NewDocuments.Single().SourcePath = "/tmp/top.pdf";
        vm.Documents.NewDocuments.Single().Nickname = "Top";

        await vm.SaveCommand.ExecuteAsync();

        patternService.AddRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Name = "Summer top",
            Url = "https://example.test",
            Type = PatternType.Tricot
        });
        documentService.AddAllRequests.Should().ContainSingle()
            .Which.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { SourcePath = "/tmp/top.pdf", Nickname = "Top", PatternId = 12 });
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Success && c.Message == "Le patron et ses documents ont été ajoutés.");
        nav.GoBackCount.Should().Be(1);
    }

    [Fact]
    public async Task PatternsForm_Edit_Loads_Existing_Documents_And_Sends_Update()
    {
        var doc = TestData.Document(nickname: "Old");
        var patternService = new FakePatternService { UpdateResult = ResultT<Pattern>.Ok(TestData.Pattern(id: 3)) };
        var documentService = new FakeDocumentService
        {
            GetAllResult = ResultT<IReadOnlyList<Document>>.Ok([doc]),
            UpdateResult = ResultT<Document>.Ok(doc)
        };
        var vm = CreateFormViewModel(patternService: patternService, documentService: documentService);

        await vm.InitEdit(3, "Old pattern", null, "note", PatternType.Crochet, true, new DateOnly(2026, 1, 1), null, [doc.Id]);
        vm.Name = "New pattern";
        vm.Documents.ExistingDocuments.Single().Nickname = "Renamed";

        await vm.SaveCommand.ExecuteAsync();

        patternService.UpdateRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Id = 3,
            Name = "New pattern",
            IsPersonal = true,
            BeginDate = new DateOnly(2026, 1, 1)
        });
        documentService.UpdateRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Id = doc.Id, Nickname = "Renamed" });
    }

    [Fact]
    public async Task PatternsForm_Document_Save_Failure_Does_Not_Navigate_Back()
    {
        var nav = new FakeNavigationService();
        var patternService = new FakePatternService { AddResult = ResultT<Pattern>.Ok(TestData.Pattern(id: 4)) };
        var documentService = new FakeDocumentService { AddAllResult = ResultT<IReadOnlyList<Document>>.Failure("upload failed") };
        var notifications = new FakeNotificationService();
        var vm = CreateFormViewModel(nav, patternService, documentService, notifications);
        vm.InitCreate();
        vm.Name = "Pattern";
        vm.Documents.NewDocuments.Single().SourcePath = "/tmp/p.pdf";

        await vm.SaveCommand.ExecuteAsync();

        nav.GoBackCount.Should().Be(0);
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Error && c.Message == "upload failed");
    }

    [Fact]
    public async Task PatternsDetail_Loads_Derived_Data_Opens_Document_And_Project()
    {
        var nav = new FakeNavigationService();
        var doc = TestData.Document();
        var linkedProject = TestData.PatternProject(id: 9, name: "Linked");
        var project = TestData.Project(id: 9);
        var projectService = new FakeProjectService();
        projectService.ByIdResults[9] = ResultT<Project>.Ok(project);
        var documentService = new FakeDocumentService();
        var vm = new PatternsDetailViewModel(
            nav,
            new FakePatternService(),
            projectService,
            documentService,
            new FakeNotificationService(),
            new FakeRefreshService());

        vm.Load(TestData.Pattern(documents: [doc], projects: [linkedProject], note: null, url: null));

        vm.Title.Should().Be("Détail patron");
        vm.HasDocuments.Should().BeTrue();
        vm.HasProjects.Should().BeTrue();
        vm.HasUrl.Should().BeFalse();
        vm.NoteDisplay.Should().Be("Aucune note.");

        await vm.Documents.Single().OpenCommand.ExecuteAsync();
        await vm.Projects.Single().OpenCommand!.ExecuteAsync();

        documentService.OpenIds.Should().ContainSingle().Which.Should().Be(doc.Id);
        nav.NavigatedTypes.Should().Contain(typeof(ProjectsDetailViewModel));
    }

    [Fact]
    public void PatternsDetail_Refresh_Failure_Sets_Error_And_Does_Not_Clear_Existing_Data()
    {
        var patternService = new FakePatternService();
        patternService.ByIdResults[2] = ResultT<Pattern>.Failure("missing");
        var vm = new PatternsDetailViewModel(
            new FakeNavigationService(),
            patternService,
            new FakeProjectService(),
            new FakeDocumentService(),
            new FakeNotificationService(),
            new FakeRefreshService());
        vm.Load(TestData.Pattern(id: 2, name: "Existing"));

        vm.OnNavigatedTo();

        vm.ErrorMessage.Should().Be("missing");
        vm.Name.Should().Be("Existing");
    }

    [Fact]
    public async Task PatternsDetail_Delete_Success_Notifies_And_Goes_Back()
    {
        var nav = new FakeNavigationService();
        var notifications = new FakeNotificationService();
        var patternService = new FakePatternService();
        var vm = new PatternsDetailViewModel(nav, patternService, new FakeProjectService(), new FakeDocumentService(), notifications, new FakeRefreshService());
        vm.Load(TestData.Pattern(id: 6));

        await vm.ConfirmDeleteCommand.ExecuteAsync();

        patternService.DeleteIds.Should().ContainSingle().Which.Should().Be(6);
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Success);
        nav.GoBackCount.Should().Be(1);
    }

    private static PatternsFormViewModel CreateFormViewModel(
        FakeNavigationService? nav = null,
        FakePatternService? patternService = null,
        FakeDocumentService? documentService = null,
        FakeNotificationService? notifications = null)
    {
        documentService ??= new FakeDocumentService();
        notifications ??= new FakeNotificationService();
        return new PatternsFormViewModel(
            nav ?? new FakeNavigationService(),
            patternService ?? new FakePatternService(),
            documentService,
            notifications,
            new DocumentsPickerFormViewModel(documentService, new FakeDocumentFilePicker(), notifications));
    }
}
