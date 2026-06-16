// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Request;
using Looma.Infrastructure.Repositories;

namespace Looma.Infrastructure.Tests.Repositories;

public sealed class DocumentRepositoryTests
{
    [Fact]
    public async Task AddAsync_copies_file_and_persists_metadata_with_default_nickname()
    {
        using var fixture = new RepositoryTestFixture();
        var source = fixture.CreateSourceFile("instructions.pdf", "abc123");
        await using var context = fixture.CreateContext();
        var repository = new DocumentRepository(context, fixture.Paths);

        var result = await repository.AddAsync(new CreateDocumentRequest(source, null));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Nickname.Should().Be("instructions");
        result.Value.Type.Should().Be("PDF");
        result.Value.SizeBytes.Should().Be(6);
        result.Value.StoragePath.Should().NotBeNull();
        File.Exists(result.Value.StoragePath).Should().BeTrue();
        Path.GetFileName(result.Value.StoragePath).Should().Be($"{result.Value.Id}.pdf");
        context.Documents.Should().ContainSingle();
    }

    [Fact]
    public async Task AddAsync_rejects_blank_or_missing_source_and_does_not_create_document()
    {
        using var fixture = new RepositoryTestFixture();
        await using var context = fixture.CreateContext();
        var repository = new DocumentRepository(context, fixture.Paths);

        var blank = await repository.AddAsync(new CreateDocumentRequest(" ", "Doc"));
        var missing = await repository.AddAsync(new CreateDocumentRequest(Path.Combine(fixture.RootPath, "missing.pdf"), "Doc"));

        blank.Status.Should().Be(ResultStatus.Failure);
        missing.Status.Should().Be(ResultStatus.NotFound);
        context.Documents.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_rejects_request_with_pattern_and_project()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var project = await fixture.AddProjectAsync(pattern.PatternId);
        var source = fixture.CreateSourceFile("doc.pdf");
        await using var context = fixture.CreateContext();
        var repository = new DocumentRepository(context, fixture.Paths);

        var result = await repository.AddAsync(new CreateDocumentRequest(source, "Doc", pattern.PatternId, project.ProjectId));

        result.Status.Should().Be(ResultStatus.Failure);
        context.Documents.Should().BeEmpty();
        Directory.EnumerateFiles(fixture.Paths.DocumentsFolder).Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_rejects_missing_parent_before_copying_file()
    {
        using var fixture = new RepositoryTestFixture();
        var source = fixture.CreateSourceFile("doc.pdf");
        await using var context = fixture.CreateContext();
        var repository = new DocumentRepository(context, fixture.Paths);

        var result = await repository.AddAsync(new CreateDocumentRequest(source, "Doc", PatternId: 404));

        result.Status.Should().Be(ResultStatus.NotFound);
        context.Documents.Should().BeEmpty();
        Directory.EnumerateFiles(fixture.Paths.DocumentsFolder).Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_backfills_missing_metadata_and_returns_file_metadata()
    {
        using var fixture = new RepositoryTestFixture();
        var document = await fixture.AddDocumentEntityAsync("Doc", type: null, size: null);
        var storagePath = Path.Combine(fixture.Paths.DocumentsFolder, $"{document.DocumentId}.txt");
        File.WriteAllText(storagePath, "hello");
        await using var context = fixture.CreateContext();
        var repository = new DocumentRepository(context, fixture.Paths);

        var result = await repository.GetAllAsync();

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Should().ContainSingle();
        result.Value![0].Type.Should().Be("TXT");
        result.Value![0].SizeBytes.Should().Be(5);
        var stored = await context.Documents.FindAsync(document.DocumentId);
        stored!.Type.Should().Be("TXT");
        stored.Size.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_trims_nickname_and_rejects_blank()
    {
        using var fixture = new RepositoryTestFixture();
        var document = await fixture.AddDocumentEntityAsync("Original");
        await using var context = fixture.CreateContext();
        var repository = new DocumentRepository(context, fixture.Paths);

        var updated = await repository.UpdateAsync(new UpdateDocumentRequest(document.DocumentId, "  Updated  "));
        var blank = await repository.UpdateAsync(new UpdateDocumentRequest(document.DocumentId, "   "));

        updated.Succeeded.Should().BeTrue(updated.Error);
        updated.Value!.Nickname.Should().Be("Updated");
        blank.Status.Should().Be(ResultStatus.Failure);
        (await context.Documents.FindAsync(document.DocumentId))!.Nickname.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteAsync_removes_database_row_and_storage_file()
    {
        using var fixture = new RepositoryTestFixture();
        var source = fixture.CreateSourceFile("doc.pdf");
        await using var context = fixture.CreateContext();
        var repository = new DocumentRepository(context, fixture.Paths);
        var added = await repository.AddAsync(new CreateDocumentRequest(source, "Doc"));
        var storagePath = added.Value!.StoragePath!;

        var result = await repository.DeleteAsync(added.Value.Id);

        result.Succeeded.Should().BeTrue(result.Error);
        (await context.Documents.FindAsync(added.Value.Id)).Should().BeNull();
        File.Exists(storagePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_deletes_file_with_extension_for_legacy_row_without_metadata()
    {
        using var fixture = new RepositoryTestFixture();
        var document = await fixture.AddDocumentEntityAsync("Legacy", type: null, size: null);
        var storagePath = Path.Combine(fixture.Paths.DocumentsFolder, $"{document.DocumentId}.pdf");
        File.WriteAllText(storagePath, "legacy");
        await using var context = fixture.CreateContext();
        var repository = new DocumentRepository(context, fixture.Paths);

        var result = await repository.DeleteAsync(document.DocumentId);

        result.Succeeded.Should().BeTrue(result.Error);
        File.Exists(storagePath).Should().BeFalse();
    }
}
