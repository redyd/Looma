// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Request;
using Looma.Infrastructure.Repositories;

namespace Looma.Infrastructure.Tests.Repositories;

public sealed class PatternRepositoryTests
{
    [Fact]
    public async Task AddAsync_trims_name_and_normalizes_optional_fields()
    {
        using var fixture = new RepositoryTestFixture();
        await using var context = fixture.CreateContext();
        var repository = new PatternRepository(context, fixture.Paths);

        var result = await repository.AddAsync(new CreatePatternRequest(
            "  Shawl  ", "  https://example.test/pattern  ", "   ", PatternType.Tricot, true));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Name.Should().Be("Shawl");
        result.Value.Url.Should().Be("https://example.test/pattern");
        result.Value.Note.Should().BeNull();
        result.Value.Type.Should().Be(PatternType.Tricot);
        result.Value.IsPersonal.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_returns_patterns_ordered_by_name_with_projects_and_documents()
    {
        using var fixture = new RepositoryTestFixture();
        var patternB = await fixture.AddPatternAsync("B pattern");
        var patternA = await fixture.AddPatternAsync("A pattern");
        await fixture.AddDocumentEntityAsync("Doc", patternId: patternA.PatternId);
        await fixture.AddProjectAsync(patternA.PatternId, name: "Project");
        await using var context = fixture.CreateContext();
        var repository = new PatternRepository(context, fixture.Paths);

        var result = await repository.GetAllAsync();

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Select(p => p.Name).Should().Equal("A pattern", "B pattern");
        result.Value!.First().Documents.Should().ContainSingle(d => d.Nickname == "Doc");
        result.Value!.First().Projects.Should().ContainSingle(p => p.Name == "Project");
        _ = patternB;
    }

    [Fact]
    public async Task AddDocumentAsync_links_unattached_document_to_pattern()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var document = await fixture.AddDocumentEntityAsync(patternId: null);
        await using var context = fixture.CreateContext();
        var repository = new PatternRepository(context, fixture.Paths);

        var result = await repository.AddDocumentAsync(pattern.PatternId, document.DocumentId);

        result.Succeeded.Should().BeTrue(result.Error);
        var stored = await context.Documents.FindAsync(document.DocumentId);
        stored!.PatternId.Should().Be(pattern.PatternId);
        stored.ProjectId.Should().BeNull();
    }

    [Fact]
    public async Task AddDocumentAsync_rejects_document_already_linked_to_project()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var project = await fixture.AddProjectAsync(pattern.PatternId);
        var document = await fixture.AddDocumentEntityAsync(projectId: project.ProjectId);
        await using var context = fixture.CreateContext();
        var repository = new PatternRepository(context, fixture.Paths);

        var result = await repository.AddDocumentAsync(pattern.PatternId, document.DocumentId);

        result.Status.Should().Be(ResultStatus.Failure);
        (await context.Documents.FindAsync(document.DocumentId))!.PatternId.Should().BeNull();
    }

    [Fact]
    public async Task RemoveDocumentAsync_unlinks_document_from_pattern()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var document = await fixture.AddDocumentEntityAsync(patternId: pattern.PatternId);
        await using var context = fixture.CreateContext();
        var repository = new PatternRepository(context, fixture.Paths);

        var result = await repository.RemoveDocumentAsync(pattern.PatternId, document.DocumentId);

        result.Succeeded.Should().BeTrue(result.Error);
        (await context.Documents.FindAsync(document.DocumentId))!.PatternId.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_removes_pattern_link_from_existing_projects()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        await fixture.AddProjectAsync(pattern.PatternId);
        var document = await fixture.AddDocumentEntityAsync(patternId: pattern.PatternId);
        var documentPath = fixture.Paths.GetDocumentStoragePath(document.DocumentId);
        await File.WriteAllTextAsync(documentPath, "pattern document");
        await using var context = fixture.CreateContext();
        var repository = new PatternRepository(context, fixture.Paths);

        var result = await repository.DeleteAsync(pattern.PatternId);

        result.Succeeded.Should().BeTrue(result.Error);
        (await context.Patterns.FindAsync(pattern.PatternId)).Should().BeNull();
        (await context.Documents.FindAsync(document.DocumentId)).Should().BeNull();
        context.Projects.Single().PatternId.Should().BeNull();
        File.Exists(documentPath).Should().BeFalse();
    }
}
