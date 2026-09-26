// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System;
using System.IO;
using System.Linq;
using Looma.Domain.Core;
using Looma.Domain.Localization;
using Looma.Domain.Logging;
using Looma.Infrastructure;
using Looma.Infrastructure.Services;
using Looma.Infrastructure.Storage;
using Looma.Presentation.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Looma.App.Services;

/// <summary>
/// Everything that must happen to the data files before the application uses them:
/// applying a staged import, detecting a corrupted database, and backing up before migrations.
/// </summary>
public sealed class StartupDataGuard(AppPaths paths, BackupService backupService, IDomainLogger logger) : IDatabaseRecovery
{
    private RestoreTransaction? _restore;

    /// <summary>Message to show once the main window is open (e.g. import applied or failed).</summary>
    public (bool Success, string Message)? PendingNotice { get; private set; }

    /// <summary>
    /// Erases the application data when the user asked for a reset. Must run before anything reads the data files.
    /// </summary>
    public void ApplyPendingReset()
    {
        var result = backupService.ApplyPendingReset();
        if (result is null)
            return;

        if (result.Failed)
        {
            logger.Log(DomainLogLevel.Error, $"Pending reset failed: {result.Error}");
            PendingNotice = (false, result.Error ?? Localizer.Get("Backup_Errors_UnableToReset"));
            return;
        }

        logger.Log(DomainLogLevel.Information, "Application data reset.");
        PendingNotice = (true, Localizer.Get("Backup_Notifications_ResetApplied"));
    }

    /// <summary>
    /// Applies a staged import. Must run before any database connection and before themes are seeded.
    /// </summary>
    public void ApplyPendingRestore()
    {
        var result = backupService.BeginPendingRestore();
        if (result is null)
            return;

        if (result.Failed)
        {
            logger.Log(DomainLogLevel.Error, $"Pending restore failed: {result.Error}");
            backupService.CancelPendingRestore();
            PendingNotice = (false, result.Error ?? Localizer.Get("Backup_Errors_UnableToImport"));
            return;
        }

        _restore = result.Value;
    }

    /// <summary>
    /// Checks and migrates the database. Returns a failure when the application cannot open it safely,
    /// in which case the recovery screen must be shown instead of the main window.
    /// </summary>
    public Result PrepareDatabase(LoomaDbContext context)
    {
        var state = DatabaseHealth.Check(paths.DatabasePath, out var error);
        if (state == DatabaseState.Corrupt && _restore is not null)
        {
            // Should not happen (the archive was verified) but never keep a broken import.
            RollbackRestore(error);
            state = DatabaseHealth.Check(paths.DatabasePath, out error);
        }

        if (state == DatabaseState.Corrupt)
        {
            logger.Log(DomainLogLevel.Error, $"Database is corrupted: {error}");
            return Result.Failure(Localizer.Format("Recovery_DatabaseCorrupt", error ?? string.Empty));
        }

        try
        {
            if (state == DatabaseState.Healthy && _restore is null && context.Database.GetPendingMigrations().Any())
            {
                var backup = backupService.CreateAutomaticBackup(BackupService.PreMigrationReason);
                if (backup.Failed)
                {
                    logger.Log(DomainLogLevel.Error, $"Pre-migration backup failed: {backup.Error}");
                    return Result.Failure(Localizer.Format("Recovery_PreMigrationBackupFailed", backup.Error ?? string.Empty));
                }
            }

            paths.EnsureDatabaseCreated(context);
        }
        catch (Exception ex)
        {
            logger.Log(DomainLogLevel.Error, "Database migration failed.", ex);
            if (_restore is not null)
            {
                RollbackRestore(ex.Message);
                return PrepareDatabaseAfterRollback(context);
            }

            return Result.Failure(Localizer.Format("Recovery_MigrationFailed", ex.Message));
        }

        if (_restore is not null)
        {
            _restore.Commit();
            _restore = null;
            PendingNotice = (true, Localizer.Get("Backup_Notifications_ImportApplied"));
        }

        return Result.Ok();
    }

    public Result SetAsideDatabase()
    {
        try
        {
            var suffix = $".corrupt-{DateTime.Now:yyyyMMddHHmmss}";
            foreach (var file in new[] { paths.DatabasePath, $"{paths.DatabasePath}-wal", $"{paths.DatabasePath}-shm", $"{paths.DatabasePath}-journal" })
            {
                if (File.Exists(file))
                    File.Move(file, file + suffix);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Failure(Localizer.Format("Recovery_UnableToSetAsideDatabase", ex.Message));
        }
    }

    private Result PrepareDatabaseAfterRollback(LoomaDbContext context)
    {
        try
        {
            paths.EnsureDatabaseCreated(context);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Failure(Localizer.Format("Recovery_MigrationFailed", ex.Message));
        }
    }

    private void RollbackRestore(string? reason)
    {
        // Pooled connections keep the restored database file open: release them before swapping files back.
        SqliteConnection.ClearAllPools();
        _restore?.Rollback();
        _restore = null;
        PendingNotice = (false, Localizer.Format("Backup_Errors_UnableToImport", reason ?? string.Empty));
    }
}
