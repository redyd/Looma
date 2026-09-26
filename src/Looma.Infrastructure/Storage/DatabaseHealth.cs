// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Microsoft.Data.Sqlite;

namespace Looma.Infrastructure.Storage;

public enum DatabaseState
{
    Missing,
    Healthy,
    Corrupt
}

/// <summary>
/// Low-level SQLite checks, usable before Entity Framework opens the database.
/// </summary>
public static class DatabaseHealth
{
    private static readonly byte[] SqliteHeader = "SQLite format 3\0"u8.ToArray();

    public static DatabaseState Check(string databasePath) => Check(databasePath, out _);

    public static DatabaseState Check(string databasePath, out string? error)
    {
        error = null;
        if (!File.Exists(databasePath))
            return DatabaseState.Missing;

        try
        {
            if (new FileInfo(databasePath).Length == 0)
                return DatabaseState.Missing;

            if (!HasSqliteHeader(databasePath))
            {
                error = "Invalid SQLite header.";
                return DatabaseState.Corrupt;
            }

            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString());
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA quick_check;";
            var result = command.ExecuteScalar() as string;
            if (string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                return DatabaseState.Healthy;

            error = result;
            return DatabaseState.Corrupt;
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            error = ex.Message;
            return DatabaseState.Corrupt;
        }
    }

    /// <summary>
    /// Copies a consistent snapshot of a (possibly open) database. Never copy the raw file:
    /// a write in progress or a journal would make the copy inconsistent.
    /// </summary>
    public static void Snapshot(string databasePath, string destinationPath)
    {
        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "VACUUM INTO $destination;";
        command.Parameters.AddWithValue("$destination", destinationPath);
        command.ExecuteNonQuery();
    }

    private static bool HasSqliteHeader(string databasePath)
    {
        using var stream = new FileStream(databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var buffer = new byte[SqliteHeader.Length];
        var read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        return read == buffer.Length && buffer.AsSpan().SequenceEqual(SqliteHeader);
    }
}
