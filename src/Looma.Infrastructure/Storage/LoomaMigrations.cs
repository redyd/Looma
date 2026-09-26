// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Storage;

public static class LoomaMigrations
{
    private static readonly Lazy<IReadOnlyList<string>> KnownMigrations = new(() =>
    {
        using var context = new LoomaDbContext(new DbContextOptionsBuilder<LoomaDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        return context.Database.GetMigrations().ToList();
    });

    /// <summary>Migrations shipped with this version of the application, oldest first.</summary>
    public static IReadOnlyList<string> Known => KnownMigrations.Value;

    public static bool IsKnown(string? migrationId) =>
        migrationId is null || Known.Contains(migrationId, StringComparer.Ordinal);

    /// <summary>Last migration applied to a database file, read without Entity Framework.</summary>
    public static string? ReadLastApplied(string databasePath)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        connection.Open();

        using var exists = connection.CreateCommand();
        exists.CommandText = "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory';";
        if (Convert.ToInt64(exists.ExecuteScalar()) == 0)
            return null;

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;";
        return command.ExecuteScalar() as string;
    }
}
