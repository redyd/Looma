// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Looma.Infrastructure.Tests;

/// <summary>
/// Une base créée par une version précédente (avant les instantanés de mouvements) doit être migrée
/// sans perte : lignes conservées, instantanés remplis, historique préservé à la suppression d'une laine.
/// </summary>
public sealed class TrackedWoolMigrationTests : IDisposable
{
    private const string PreviousMigration = "20260621094556_TrackedWool";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "looma-migration-tests", Guid.NewGuid().ToString("N"));
    private readonly string _databasePath;

    public TrackedWoolMigrationTests()
    {
        Directory.CreateDirectory(_root);
        _databasePath = Path.Combine(_root, "looma.db");
    }

    [Fact]
    public async Task Migration_Backfills_Snapshots_And_Keeps_Rows()
    {
        await using (var context = CreateContext())
        {
            context.GetService<IMigrator>().Migrate(PreviousMigration);
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Patterns (PatternId, Name, IsPersonal, Type) VALUES (1, 'Patron', 0, 'Tricot');
                INSERT INTO Wools (WoolId, Name, Brand, Material, Color, Weight, Length, Stock, NeedleMinSize, NeedleMaxSize)
                    VALUES (1, 'Alpaca', 'Drops', 'Alpaga', '#FFFFFF|#000000', 50, 150, 2000, 4, 4.75);
                INSERT INTO Projects (ProjectId, Name, Status, PatternId) VALUES (1, 'Pull', 2, 1);
                INSERT INTO TrackedWools (Id, Date, Quantity, WoolId, ProjectId) VALUES ('used', '2026-05-01 10:00:00', -1500, 1, 1);
                INSERT INTO TrackedWools (Id, Date, Quantity, WoolId, ProjectId) VALUES ('bought', '2026-04-01 10:00:00', 3000, 1, NULL);
                """);
        }

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();

            var rows = await context.TrackedWools.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
            rows.Select(r => r.Id).Should().Equal("bought", "used");
            rows.Should().AllSatisfy(r =>
            {
                r.WoolId.Should().Be(1);
                r.WoolName.Should().Be("Alpaca");
                r.WoolBrand.Should().Be("Drops");
                r.WoolMaterial.Should().Be("Alpaga");
                r.WoolColor.Should().Be("#FFFFFF|#000000");
                r.WoolWeight.Should().Be(50);
                r.WoolLength.Should().Be(150);
                r.WoolNeedleMinSize.Should().Be(4);
                r.WoolNeedleMaxSize.Should().Be(4.75);
            });

            var used = rows.Single(r => r.Id == "used");
            used.ProjectName.Should().Be("Pull");
            used.PatternType.Should().Be(PatternType.Tricot);
            rows.Single(r => r.Id == "bought").ProjectName.Should().BeNull();
        }

        await using (var context = CreateContext())
        {
            (await new WoolRepository(context).DeleteAsync(1)).Succeeded.Should().BeTrue();
        }

        await using (var context = CreateContext())
        {
            var movements = await new TrackedWoolRepository(context).GetMovementsAsync();
            movements.Value.Should().HaveCount(2);
            movements.Value!.Should().AllSatisfy(m =>
            {
                m.WoolExists.Should().BeFalse();
                m.WoolName.Should().Be("Alpaca");
                m.WoolColors.Should().Equal("#FFFFFF", "#000000");
            });
        }
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private LoomaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<LoomaDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options);
}
