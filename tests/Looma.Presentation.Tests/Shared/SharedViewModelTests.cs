// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Request;
using Looma.Presentation.ViewModels.Sections.Documents;
using Looma.Presentation.ViewModels.Sections.Projects;
using Looma.Presentation.ViewModels.Shared.Documents;
using Looma.Presentation.ViewModels.Shared.Patterns;
using Looma.Presentation.ViewModels.Shared.Projects;
using Looma.Presentation.Tests.TestSupport;
using CommunityToolkit.Mvvm.Input;

namespace Looma.Presentation.Tests.Shared;

public sealed class SharedViewModelTests
{
    [Fact]
    public void Summary_ViewModels_Expose_Formatted_Domain_Data()
    {
        var wool = TestData.Wool(stock: 2500);
        var pattern = TestData.Pattern(
            beginDate: new DateOnly(2026, 1, 2),
            endDate: new DateOnly(2026, 2, 3));
        var project = TestData.Project(
            status: Status.Paused,
            pattern: pattern,
            beginDate: new DateOnly(2026, 3, 4),
            wools: [TestData.WoolUsage(wool)]);
        var document = TestData.Document(sizeBytes: 2048, patternId: pattern.Id);

        new PatternSummaryViewModel(pattern, 1, 2, true, new RelayCommand(() => { }))
            .Should().BeEquivalentTo(new
            {
                HasBeginDate = true,
                BeginDateDisplay = "02/01/2026",
                HasEndDate = true,
                EndDateDisplay = "03/02/2026",
                TypeDisplay = "Crochet"
            });

        new ProjectSummaryViewModel(project, new RelayCommand(() => { }))
            .Should().BeEquivalentTo(new
            {
                StatusDisplay = "En pause",
                HasPatternType = true,
                PatternTypeDisplay = "Crochet",
                PatternName = "Cardigan",
                HasBeginDate = true,
                BeginDateDisplay = "04/03/2026",
                HasEndDate = false,
                EndDateDisplay = "Aucune",
                WoolCountDisplay = "1 laine(s)"
            });

        new DocumentSummaryViewModel(document, new RelayCommand(() => { }))
            .Should().BeEquivalentTo(new
            {
                TypeDisplay = "pdf",
                SizeDisplay = "2 KB",
                HasOrigin = true,
                OriginTypeDisplay = "Patron"
            });

        new ProjectSelectableWoolViewModel(wool, true, new RelayCommand(() => { }))
            .Should().BeEquivalentTo(new
            {
                DetailDisplay = "Drops - Alpaca",
                StockDisplay = "2.50 pelote(s)",
                SelectionDisplay = "Sélectionnée"
            });
    }

    [Fact]
    public void ProjectWoolUsageViewModel_Recomputes_Displays_When_Mode_Changes()
    {
        var vm = new ProjectWoolUsageViewModel(
            TestData.WoolUsage(TestData.Wool(weight: 50, length: 100, stock: 2000), stockUsed: 1000, stockAlreadyUsed: 500),
            new RelayCommand(() => { }));

        vm.AvailableDisplay.Should().Be("2.00 pelote(s)");
        vm.UsedDisplay.Should().Be("1.00 pelote(s)");
        vm.AlreadyDeductedDisplay.Should().Be("0.50 pelote(s)");

        vm.DisplayMode = StockAdjustmentMode.ByWeight;

        vm.AvailableDisplay.Should().Be("100 g");
        vm.UsedDisplay.Should().Be("50 g");
        vm.AlreadyDeductedDisplay.Should().Be("25 g");
    }

    [Fact]
    public async Task DocumentDraft_BrowseFile_Sets_Path_And_Default_Nickname()
    {
        var picker = new FakeDocumentFilePicker { NextPick = "/tmp/My Pattern.pdf" };
        var vm = new DocumentDraftViewModel(picker, DocumentPickerMode.All);

        await vm.BrowseFileCommand.ExecuteAsync();

        vm.SourcePath.Should().Be("/tmp/My Pattern.pdf");
        vm.Nickname.Should().Be("My Pattern");
        vm.SelectedFileName.Should().Be("My Pattern.pdf");
        vm.SelectedFileDirectory.Should().Be("/tmp");
    }

    [Fact]
    public async Task DocumentsPicker_Save_Creates_Renames_And_Deletes_Documents()
    {
        var existing = TestData.Document(nickname: "Old");
        var documentService = new FakeDocumentService
        {
            GetAllResult = ResultT<IReadOnlyList<Looma.Domain.Entities.Document>>.Ok([existing])
        };
        var notifications = new FakeNotificationService();
        var vm = new DocumentsPickerFormViewModel(documentService, new FakeDocumentFilePicker(), notifications);
        var createdRequests = new List<CreateDocumentRequest>();
        var renamed = new List<(Guid Id, string Nickname)>();

        var initialized = await vm.InitEditAsync(
            [existing.Id],
            requests =>
            {
                createdRequests.AddRange(requests);
                return Task.FromResult<ResultBase>(Result.Ok());
            },
            (id, nickname) =>
            {
                renamed.Add((id, nickname));
                return Task.FromResult<ResultBase>(Result.Ok());
            });
        initialized.Should().BeTrue();
        vm.ExistingDocuments.Single().Nickname = "Renamed";
        vm.NewDocuments.Single().SourcePath = "/tmp/new.pdf";
        vm.NewDocuments.Single().Nickname = "";

        var saved = await vm.SaveAsync();

        saved.Should().BeTrue();
        renamed.Should().ContainSingle().Which.Should().Be((existing.Id, "Renamed"));
        createdRequests.Should().ContainSingle().Which.Should().BeEquivalentTo(new CreateDocumentRequest("/tmp/new.pdf", "new"));
        vm.NewDocuments.Single().SourcePath.Should().BeNull();
    }

    [Fact]
    public async Task DocumentsPicker_Remove_Last_New_Draft_Resets_It()
    {
        var vm = new DocumentsPickerFormViewModel(new FakeDocumentService(), new FakeDocumentFilePicker(), new FakeNotificationService());
        vm.InitCreate(_ => Task.FromResult<ResultBase>(Result.Ok()));
        vm.NewDocuments.Single().SourcePath = "/tmp/a.pdf";
        vm.NewDocuments.Single().Nickname = "A";

        vm.NewDocuments.Single().RemoveCommand.Execute(null);

        vm.NewDocuments.Should().ContainSingle();
        vm.NewDocuments.Single().SourcePath.Should().BeNull();
        vm.NewDocuments.Single().Nickname.Should().BeEmpty();
        vm.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public void ProjectImageDraft_Updates_File_Display_When_Source_Changes()
    {
        var vm = new ProjectImageDraftViewModel("/tmp/first.png");

        vm.SelectedFileName.Should().Be("first.png");
        vm.Nickname.Should().Be("first");

        vm.SourcePath = "/home/user/second.jpg";

        vm.SelectedFileName.Should().Be("second.jpg");
        vm.SelectedFileDirectory.Should().Be("/home/user");
    }

}
