// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.App.Services;
using Looma.Domain.Logging;
using Looma.Infrastructure;
using Looma.Infrastructure.Entity;
using Looma.Infrastructure.Services;
using Looma.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Looma.App.Tests.Services;

public sealed class StartupDataGuardTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "looma-startup-tests", Guid.NewGuid().ToString("N"));
    private readonly AppPaths _paths;
    private readonly BackupService _backup;
    private readonly StartupDataGuard _guard;

    public StartupDataGuardTests()
    {
        _paths = new AppPaths(_root);
        _paths.EnsureDirectoriesExist();
        _backup = new BackupService(_paths, new AppConfigStore(_paths));
        _guard = new StartupDataGuard(_paths, _backup, NullDomainLogger.Instance);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void PrepareDatabase_creates_new_database_without_backup()
    {
        using var context = CreateContext();

        var result = _guard.PrepareDatabase(context);

        result.Succeeded.Should().BeTrue(result.Error);
        _backup.ListAutomaticBackups().Should().BeEmpty();
    }

    [Fact]
    public void PrepareDatabase_backs_up_before_applying_pending_migrations()
    {
        using (var context = CreateContext())
            context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().Migrate(LoomaMigrations.Known[^2]);
        SqliteConnection.ClearAllPools();

        using var migrating = CreateContext();
        var result = _guard.PrepareDatabase(migrating);

        result.Succeeded.Should().BeTrue(result.Error);
        _backup.ListAutomaticBackups().Should().ContainSingle(b => b.Reason == BackupService.PreMigrationReason);
        migrating.Database.GetPendingMigrations().Should().BeEmpty();
    }

    [Fact]
    public void PrepareDatabase_refuses_corrupted_database_and_set_aside_keeps_it()
    {
        File.WriteAllText(_paths.DatabasePath, "this is not a database, just some damaged bytes....");
        using var context = CreateContext();

        var result = _guard.PrepareDatabase(context);

        result.Failed.Should().BeTrue();
        File.ReadAllText(_paths.DatabasePath).Should().StartWith("this is not");

        _guard.SetAsideDatabase().Succeeded.Should().BeTrue();
        File.Exists(_paths.DatabasePath).Should().BeFalse();
        Directory.EnumerateFiles(_root, "looma.db.corrupt-*").Should().ContainSingle();
    }

    [Fact]
    public void ApplyPendingRestore_then_PrepareDatabase_commits_import()
    {
        using (var context = CreateContext())
        {
            context.Database.Migrate();
            context.Wools.Add(new WoolEntity
            {
                Name = "From backup", Brand = "B", Material = "M", Color = "C",
                Weight = 100, Length = 200, Stock = 10, NeedleMinSize = 4, NeedleMaxSize = 5
            });
            context.SaveChanges();
        }

        var archive = Path.Combine(_root, "backup.looma");
        _backup.Export(archive).Succeeded.Should().BeTrue();
        using (var context = CreateContext())
        {
            context.Wools.RemoveRange(context.Wools);
            context.SaveChanges();
        }

        _backup.StageRestore(archive).Succeeded.Should().BeTrue();
        SqliteConnection.ClearAllPools();

        _guard.ApplyPendingRestore();
        using var restored = CreateContext();
        var result = _guard.PrepareDatabase(restored);

        result.Succeeded.Should().BeTrue(result.Error);
        restored.Wools.Select(w => w.Name).Should().Equal("From backup");
        _guard.PendingNotice.Should().NotBeNull();
        _guard.PendingNotice!.Value.Success.Should().BeTrue();
        _backup.HasPendingRestore.Should().BeFalse();
    }

    private LoomaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<LoomaDbContext>()
            .UseSqlite($"Data Source={_paths.DatabasePath}")
            .Options);
}
