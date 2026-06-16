// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Request;
using Looma.Infrastructure.Repositories;

namespace Looma.Infrastructure.Tests.Repositories;

public sealed class ProjectRepositoryTests
{
    [Fact]
    public async Task AddAsync_creates_project_with_distinct_wools_and_normalized_note()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync("Pattern");
        var wool1 = await fixture.AddWoolAsync("A", "Brand");
        var wool2 = await fixture.AddWoolAsync("B", "Brand");
        await using var context = fixture.CreateContext();
        var repository = new ProjectRepository(context, fixture.Paths);

        var result = await repository.AddAsync(new CreateProjectRequest(
            "  Blanket  ",
            Status.InProgress,
            "   ",
            new DateOnly(2026, 1, 1),
            null,
            pattern.PatternId,
            [wool1.WoolId, wool1.WoolId, wool2.WoolId]));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Name.Should().Be("Blanket");
        result.Value.Note.Should().BeNull();
        result.Value.Pattern!.Id.Should().Be(pattern.PatternId);
        result.Value.Wools.Select(w => w.Wool.Id).Should().BeEquivalentTo([wool1.WoolId, wool2.WoolId]);
        context.WoolsForProjects.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddAsync_allows_missing_pattern_and_rejects_missing_wool()
    {
        using var fixture = new RepositoryTestFixture();
        await using var context = fixture.CreateContext();
        var repository = new ProjectRepository(context, fixture.Paths);

        var withoutPattern = await repository.AddAsync(new CreateProjectRequest(
            "Project", Status.InProgress, null, null, null, null, []));

        withoutPattern.Succeeded.Should().BeTrue(withoutPattern.Error);
        withoutPattern.Value!.Pattern.Should().BeNull();

        var pattern = await fixture.AddPatternAsync();
        var missingWool = await repository.AddAsync(new CreateProjectRequest(
            "Project", Status.InProgress, null, null, null, pattern.PatternId, [404]));

        missingWool.Status.Should().Be(ResultStatus.Failure);
    }

    [Fact]
    public async Task GetAllAsync_orders_started_projects_first_then_begin_date_then_name()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        await using (var seed = fixture.CreateContext())
        {
            seed.Projects.AddRange(
                new() { Name = "No date", Status = Status.Wishlist, PatternId = pattern.PatternId },
                new() { Name = "B started", Status = Status.InProgress, BeginDate = new DateOnly(2026, 2, 1), PatternId = pattern.PatternId },
                new() { Name = "A started", Status = Status.InProgress, BeginDate = new DateOnly(2026, 2, 1), PatternId = pattern.PatternId },
                new() { Name = "Earlier", Status = Status.InProgress, BeginDate = new DateOnly(2026, 1, 1), PatternId = pattern.PatternId });
            await seed.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new ProjectRepository(context, fixture.Paths);

        var result = await repository.GetAllAsync();

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Select(p => p.Name).Should().Equal("Earlier", "A started", "B started", "No date");
    }

    [Fact]
    public async Task UpdateAsync_synchronizes_wool_links_and_preserves_existing_usage_values()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var wool1 = await fixture.AddWoolAsync("A", "Brand");
        var wool2 = await fixture.AddWoolAsync("B", "Brand");
        var wool3 = await fixture.AddWoolAsync("C", "Brand");
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool1.WoolId, wool2.WoolId]);
        await using (var seed = fixture.CreateContext())
        {
            var usage = await seed.WoolsForProjects.FindAsync(wool2.WoolId, project.ProjectId);
            usage!.StockUsed = 12;
            usage.StockAlreadyUsed = 7;
            await seed.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new ProjectRepository(context, fixture.Paths);

        var result = await repository.UpdateAsync(new UpdateProjectRequest(
            project.ProjectId,
            "Updated",
            Status.Paused,
            " note ",
            null,
            null,
            pattern.PatternId,
            [wool2.WoolId, wool3.WoolId]));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Wools.Select(w => w.Wool.Id).Should().BeEquivalentTo([wool2.WoolId, wool3.WoolId]);
        var preserved = await context.WoolsForProjects.FindAsync(wool2.WoolId, project.ProjectId);
        preserved!.StockUsed.Should().Be(12);
        preserved.StockAlreadyUsed.Should().Be(7);
        (await context.WoolsForProjects.FindAsync(wool1.WoolId, project.ProjectId)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_removes_project_join_rows_and_attached_documents()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var wool = await fixture.AddWoolAsync();
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool.WoolId]);
        var document = await fixture.AddDocumentEntityAsync(projectId: project.ProjectId);
        var documentPath = fixture.Paths.GetDocumentStoragePath(document.DocumentId);
        await File.WriteAllTextAsync(documentPath, "project image");
        await using var context = fixture.CreateContext();
        var repository = new ProjectRepository(context, fixture.Paths);

        var result = await repository.DeleteAsync(project.ProjectId);

        result.Succeeded.Should().BeTrue(result.Error);
        (await context.Projects.FindAsync(project.ProjectId)).Should().BeNull();
        (await context.Documents.FindAsync(document.DocumentId)).Should().BeNull();
        context.WoolsForProjects.Should().BeEmpty();
        File.Exists(documentPath).Should().BeFalse();
    }
}
