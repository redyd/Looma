// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Refresh;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Sections.Documents;
using Looma.Presentation.ViewModels.Sections.Patterns;
using Looma.Presentation.ViewModels.Sections.Projects;
using Looma.Presentation.Tests.TestSupport;

namespace Looma.Presentation.Tests.Sections.Documents;

public sealed class DocumentsViewModelTests
{
    [Fact]
    public void DocumentsForm_InitEdit_Sets_Display_Properties_And_Save_State()
    {
        var id = Guid.NewGuid();
        var vm = new DocumentsFormViewModel(new FakeNavigationService(), new FakeDocumentService(), new FakeNotificationService());

        vm.InitEdit(id, "Invoice");
        vm.SourcePath = "/tmp/invoice.pdf";

        vm.Title.Should().Be("Modifier le document");
        vm.Nickname.Should().Be("Invoice");
        vm.SelectedFileName.Should().Be("invoice.pdf");
        vm.SelectedFileDirectory.Should().Be("/tmp");
        vm.SaveCommand.CanExecute(null).Should().BeTrue();

        vm.Nickname = " ";
        vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task DocumentsForm_Save_Updates_Document_Notifies_And_Goes_Back()
    {
        var nav = new FakeNavigationService();
        var documentService = new FakeDocumentService();
        var notifications = new FakeNotificationService();
        var id = Guid.NewGuid();
        var vm = new DocumentsFormViewModel(nav, documentService, notifications);
        vm.InitEdit(id, "Old");
        vm.Nickname = "New";

        await vm.SaveCommand.ExecuteAsync();

        documentService.UpdateRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Id = id, Nickname = "New" });
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Success);
        nav.GoBackCount.Should().Be(1);
    }

    [Fact]
    public async Task DocumentsForm_Save_Failure_Notifies_And_Stays_On_Page()
    {
        var nav = new FakeNavigationService();
        var documentService = new FakeDocumentService { UpdateResult = ResultT<Document>.Failure("invalid") };
        var notifications = new FakeNotificationService();
        var vm = new DocumentsFormViewModel(nav, documentService, notifications);
        vm.InitEdit(Guid.NewGuid(), "Doc");

        await vm.SaveCommand.ExecuteAsync();

        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Error && c.Message == "invalid");
        nav.GoBackCount.Should().Be(0);
    }

    [Fact]
    public async Task DocumentsList_Loads_Searches_Opens_And_Deletes_Documents()
    {
        var doc = TestData.Document(nickname: "Gauge", type: "pdf");
        var documentService = new FakeDocumentService
        {
            GetAllResult = ResultT<IReadOnlyList<Document>>.Ok([doc, TestData.Document(nickname: "Photo", type: "png")])
        };
        var notifications = new FakeNotificationService();
        var refresh = new FakeRefreshService();
        var vm = new DocumentsListViewModel(
            new FakeNavigationService(),
            documentService,
            new FakePatternService(),
            new FakeProjectService(),
            notifications,
            refresh);

        vm.OnNavigatedTo();

        vm.Title.Should().Be("Mes documents");
        vm.CurrentPageEntities.Should().HaveCount(2);
        vm.SearchQuery = "gauge pdf";
        vm.CurrentPageEntities.Should().ContainSingle(s => s.Document.Id == doc.Id);

        await vm.CurrentPageEntities.Single().OpenCommand.ExecuteAsync();
        await vm.CurrentPageEntities.Single().DeleteCommand!.ExecuteAsync();

        documentService.OpenIds.Should().ContainSingle().Which.Should().Be(doc.Id);
        documentService.DeleteIds.Should().ContainSingle().Which.Should().Be(doc.Id);
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Success && c.Message == "Le document a été supprimé.");

        refresh.RequestRefresh(RefreshScope.Documents, "test");
        documentService.GetAllCalls.Should().Be(2);
    }

    [Fact]
    public async Task DocumentsList_OpenOrigin_Navigates_To_Pattern_When_Document_Has_Pattern()
    {
        var nav = new FakeNavigationService();
        var pattern = TestData.Pattern(id: 8);
        var doc = TestData.Document(patternId: pattern.Id);
        var patternService = new FakePatternService();
        patternService.ByIdResults[pattern.Id] = ResultT<Pattern>.Ok(pattern);
        var vm = new DocumentsListViewModel(
            nav,
            new FakeDocumentService { GetAllResult = ResultT<IReadOnlyList<Document>>.Ok([doc]) },
            patternService,
            new FakeProjectService(),
            new FakeNotificationService(),
            new FakeRefreshService());
        vm.OnNavigatedTo();

        await vm.CurrentPageEntities.Single().OpenOriginCommand!.ExecuteAsync();

        nav.NavigatedTypes.Should().Contain(typeof(PatternsDetailViewModel));
    }

    [Fact]
    public async Task DocumentsList_OpenOrigin_Navigates_To_Project_When_Document_Has_Project()
    {
        var nav = new FakeNavigationService();
        var project = TestData.Project(id: 9);
        var doc = TestData.Document(projectId: project.ProjectId);
        var projectService = new FakeProjectService();
        projectService.ByIdResults[project.ProjectId] = ResultT<Project>.Ok(project);
        var vm = new DocumentsListViewModel(
            nav,
            new FakeDocumentService { GetAllResult = ResultT<IReadOnlyList<Document>>.Ok([doc]) },
            new FakePatternService(),
            projectService,
            new FakeNotificationService(),
            new FakeRefreshService());
        vm.OnNavigatedTo();

        await vm.CurrentPageEntities.Single().OpenOriginCommand!.ExecuteAsync();

        nav.NavigatedTypes.Should().Contain(typeof(ProjectsDetailViewModel));
    }

    [Fact]
    public void DocumentsList_Load_Failure_Clears_List_And_Notifies()
    {
        var notifications = new FakeNotificationService();
        var vm = new DocumentsListViewModel(
            new FakeNavigationService(),
            new FakeDocumentService { GetAllResult = ResultT<IReadOnlyList<Document>>.Failure("load failed") },
            new FakePatternService(),
            new FakeProjectService(),
            notifications,
            new FakeRefreshService());

        vm.OnNavigatedTo();

        vm.CurrentPageEntities.Should().BeEmpty();
        notifications.Calls.Should().Contain(c => c.Severity == NotificationSeverity.Error && c.Message == "load failed");
    }
}
