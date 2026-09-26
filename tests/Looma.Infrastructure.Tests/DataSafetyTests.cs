// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Request;
using Looma.Infrastructure.Repositories;
using Looma.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Looma.Infrastructure.Tests;

public sealed class DataSafetyTests
{
    [Fact]
    public async Task UnitOfWork_commits_all_changes_when_result_succeeds()
    {
        using var fixture = new RepositoryTestFixture();
        var wool = await fixture.AddWoolAsync(stock: 1000);
        await using var context = fixture.CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var wools = new WoolRepository(context);
        var tracked = new TrackedWoolRepository(context);

        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            await wools.AddStock(wool.WoolId, -200);
            return await tracked.AddAsync(wool.WoolId, -200);
        });

        result.Succeeded.Should().BeTrue(result.Error);
        await using var verify = fixture.CreateContext();
        (await verify.Wools.SingleAsync()).Stock.Should().Be(800);
        verify.TrackedWools.Should().ContainSingle();
    }

    [Fact]
    public async Task UnitOfWork_rolls_back_every_change_when_a_later_step_fails()
    {
        using var fixture = new RepositoryTestFixture();
        var wool = await fixture.AddWoolAsync(stock: 1000);
        await using var context = fixture.CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var wools = new WoolRepository(context);
        var tracked = new TrackedWoolRepository(context);

        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            await wools.AddStock(wool.WoolId, -200);
            return await tracked.AddAsync(wool.WoolId, -200, projectId: 999);
        });

        result.Status.Should().Be(ResultStatus.NotFound);
        await using var verify = fixture.CreateContext();
        (await verify.Wools.SingleAsync()).Stock.Should().Be(1000);
        verify.TrackedWools.Should().BeEmpty();
        (await context.Wools.SingleAsync()).Stock.Should().Be(1000, "rolled-back values must not stay cached");
    }

    [Fact]
    public async Task UnitOfWork_rolls_back_when_action_throws()
    {
        using var fixture = new RepositoryTestFixture();
        var wool = await fixture.AddWoolAsync(stock: 1000);
        await using var context = fixture.CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var wools = new WoolRepository(context);

        var act = () => unitOfWork.ExecuteAsync<Result>(async () =>
        {
            await wools.AddStock(wool.WoolId, -200);
            throw new InvalidOperationException("boom");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        await using var verify = fixture.CreateContext();
        (await verify.Wools.SingleAsync()).Stock.Should().Be(1000);
    }

    [Fact]
    public async Task SaveChanges_rejects_non_finite_numbers_and_does_not_poison_later_saves()
    {
        using var fixture = new RepositoryTestFixture();
        var wool = await fixture.AddWoolAsync();
        await using var context = fixture.CreateContext();
        var repository = new TrackedWoolRepository(context);

        var invalid = await repository.AddAsync(wool.WoolId, double.NaN);
        var valid = await repository.AddAsync(wool.WoolId, 50);

        invalid.Failed.Should().BeTrue();
        valid.Succeeded.Should().BeTrue(valid.Error);
        await using var verify = fixture.CreateContext();
        verify.TrackedWools.Should().ContainSingle().Which.Quantity.Should().Be(50);
    }

    [Fact]
    public async Task DocumentDelete_keeps_file_when_database_save_fails()
    {
        using var fixture = new RepositoryTestFixture();
        var source = fixture.CreateSourceFile("keep.pdf");
        Guid id;
        string storagePath;
        await using (var context = fixture.CreateContext())
        {
            var added = await new DocumentRepository(context, fixture.Paths).AddAsync(new CreateDocumentRequest(source, "Doc"));
            id = added.Value!.Id;
            storagePath = added.Value.StoragePath!;
        }

        await using (var failing = CreateFailingContext(fixture))
        {
            var result = await new DocumentRepository(failing, fixture.Paths).DeleteAsync(id);
            result.Failed.Should().BeTrue();
        }

        File.Exists(storagePath).Should().BeTrue();
        await using var verify = fixture.CreateContext();
        verify.Documents.Should().ContainSingle();
    }

    [Fact]
    public async Task PatternDelete_keeps_document_files_when_database_save_fails()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var source = fixture.CreateSourceFile("keep.pdf");
        string storagePath;
        await using (var context = fixture.CreateContext())
        {
            var added = await new DocumentRepository(context, fixture.Paths)
                .AddAsync(new CreateDocumentRequest(source, "Doc", PatternId: pattern.PatternId));
            storagePath = added.Value!.StoragePath!;
        }

        await using (var failing = CreateFailingContext(fixture))
        {
            var result = await new PatternRepository(failing, fixture.Paths).DeleteAsync(pattern.PatternId);
            result.Failed.Should().BeTrue();
        }

        File.Exists(storagePath).Should().BeTrue();
    }

    [Fact]
    public void AtomicFile_WriteAllText_replaces_content_without_leaving_temp_files()
    {
        using var fixture = new RepositoryTestFixture();
        var path = Path.Combine(fixture.RootPath, "atomic.json");

        AtomicFile.WriteAllText(path, "first");
        AtomicFile.WriteAllText(path, "second");

        File.ReadAllText(path).Should().Be("second");
        Directory.EnumerateFiles(fixture.RootPath, "*.tmp").Should().BeEmpty();
    }

    [Fact]
    public void AtomicFile_keeps_previous_content_when_writer_fails()
    {
        using var fixture = new RepositoryTestFixture();
        var path = Path.Combine(fixture.RootPath, "atomic.json");
        AtomicFile.WriteAllText(path, "original");

        var act = () => AtomicFile.Write(path, stream =>
        {
            stream.Write("partial"u8);
            throw new IOException("disk full");
        });

        act.Should().Throw<IOException>();
        File.ReadAllText(path).Should().Be("original");
        Directory.EnumerateFiles(fixture.RootPath, "*.tmp").Should().BeEmpty();
    }

    private static LoomaDbContext CreateFailingContext(RepositoryTestFixture fixture)
    {
        var options = new DbContextOptionsBuilder<LoomaDbContext>(fixture.Options)
            .AddInterceptors(new FailingSaveInterceptor())
            .Options;
        return new LoomaDbContext(options);
    }

    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("Simulated database failure.");
    }
}
