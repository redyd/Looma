// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Looma.Domain.Backup;
using Looma.Domain.Core;
using Looma.Domain.IServices;
using Looma.Domain.Localization;
using Looma.Infrastructure.Storage;

namespace Looma.Infrastructure.Services;

public sealed partial class BackupService(AppPaths paths, AppConfigStore configStore) : IBackupService
{
    public const string FileExtension = ".looma";
    public const string PreMigrationReason = "pre-migration";
    public const string PreImportReason = "pre-import";
    public const string PreResetReason = "pre-reset";
    public const int AutomaticBackupsKept = 5;

    private const string ManifestPath = "manifest.json";
    private const string DatabaseEntry = "looma.db";
    private const string ConfigEntry = "config.json";
    private const string ReadyMarker = "ready";
    private const string ContentFolder = "content";
    private const int MaxEntries = 100_000;
    private const long MaxManifestSize = 16L * 1024 * 1024;
    private const long MaxTotalSize = 32L * 1024 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public string BackupsFolder => paths.BackupsFolder;

    public Task<ResultT<BackupManifest>> ExportAsync(string destinationPath) =>
        Task.Run(() => Export(destinationPath));

    public Task<ResultT<BackupManifest>> ValidateAsync(string archivePath) =>
        Task.Run(() => Validate(archivePath));

    public Task<ResultT<BackupManifest>> StageRestoreAsync(string archivePath) =>
        Task.Run(() => StageRestore(archivePath));

    public IReadOnlyList<BackupInfo> ListAutomaticBackups()
    {
        if (!Directory.Exists(paths.BackupsFolder))
            return [];

        return Directory
            .EnumerateFiles(paths.BackupsFolder, $"*{FileExtension}", SearchOption.TopDirectoryOnly)
            .Select(ToBackupInfo)
            .OrderByDescending(backup => backup.CreatedAt)
            .ToList();
    }

    public bool HasPendingRestore => File.Exists(Path.Combine(paths.PendingRestoreFolder, ReadyMarker));

    public bool HasPendingReset => File.Exists(paths.PendingResetMarker);

    public Task<Result> ScheduleResetAsync()
    {
        try
        {
            // A reset supersedes any import waiting for the next start.
            CancelPendingRestore();
            AtomicFile.WriteAllText(paths.PendingResetMarker, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure(Localizer.Format("Backup_Errors_UnableToReset", ex.Message)));
        }
    }

    public void CancelPendingReset() => TryDeleteFile(paths.PendingResetMarker);

    /// <summary>
    /// Erases the application data when a reset is pending. Must run before any database connection is opened.
    /// The current data is backed up first; if that is impossible (e.g. damaged database) it is moved aside instead.
    /// Returns null when no reset is pending.
    /// </summary>
    public Result? ApplyPendingReset()
    {
        if (!HasPendingReset)
            return null;

        try
        {
            var databaseState = DatabaseHealth.Check(paths.DatabasePath);
            var backedUp = false;
            if (databaseState == DatabaseState.Healthy)
            {
                var backup = CreateAutomaticBackup(PreResetReason);
                if (backup.Failed)
                {
                    CancelPendingReset();
                    return Result.Failure(Localizer.Format("Backup_Errors_UnableToCreateSafetyBackup", backup.Error ?? string.Empty));
                }

                backedUp = true;
            }

            if (backedUp)
            {
                foreach (var file in DatabaseFiles(paths.DatabasePath))
                    TryDeleteFile(file);
                TryDeleteDirectory(paths.DocumentsFolder);
                TryDeleteDirectory(paths.ThemesFolder);
            }
            else
            {
                // Nothing could be backed up: keep every file aside rather than erasing it.
                var previousFolder = Path.Combine(paths.RootPath, $"reset-previous-{DateTime.Now:yyyyMMddHHmmss}");
                Directory.CreateDirectory(previousFolder);
                foreach (var file in DatabaseFiles(paths.DatabasePath))
                    MoveIfExists(file, Path.Combine(previousFolder, Path.GetFileName(file)));
                MoveIfExists(paths.DocumentsFolder, Path.Combine(previousFolder, "documents"));
                MoveIfExists(paths.ThemesFolder, Path.Combine(previousFolder, "themes"));
            }

            configStore.Update(config =>
            {
                config.SelectedTheme = null;
                config.SelectedLanguage = null;
            });

            CancelPendingRestore();
            CancelPendingReset();
            paths.EnsureDirectoriesExist();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            CancelPendingReset();
            return Result.Failure(Localizer.Format("Backup_Errors_UnableToReset", ex.Message));
        }
    }

    public ResultT<BackupManifest> Export(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
            return ResultT<BackupManifest>.Failure(Localizer.Get("Backup_Errors_InvalidDestination"));

        var workFolder = CreateWorkFolder();
        var tempArchive = $"{destinationPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var files = new List<(string EntryPath, string SourcePath)>();

            string? lastMigration = null;
            if (DatabaseHealth.Check(paths.DatabasePath) == DatabaseState.Healthy)
            {
                var snapshot = Path.Combine(workFolder, DatabaseEntry);
                DatabaseHealth.Snapshot(paths.DatabasePath, snapshot);
                lastMigration = LoomaMigrations.ReadLastApplied(snapshot);
                files.Add((DatabaseEntry, snapshot));
            }
            else if (File.Exists(paths.DatabasePath))
            {
                return ResultT<BackupManifest>.Failure(Localizer.Get("Backup_Errors_DatabaseCorrupt"));
            }

            if (Directory.Exists(paths.DocumentsFolder))
            {
                files.AddRange(Directory
                    .EnumerateFiles(paths.DocumentsFolder, "*", SearchOption.TopDirectoryOnly)
                    .Where(path => IsAllowedEntry($"documents/{Path.GetFileName(path)}"))
                    .Select(path => ($"documents/{Path.GetFileName(path)}", path)));
            }

            if (Directory.Exists(paths.ThemesFolder))
            {
                files.AddRange(Directory
                    .EnumerateFiles(paths.ThemesFolder, "*.json", SearchOption.TopDirectoryOnly)
                    .Where(path => IsAllowedEntry($"themes/{Path.GetFileName(path)}") && AppConfigStore.IsValidJson(path))
                    .Select(path => ($"themes/{Path.GetFileName(path)}", path)));
            }

            var config = configStore.Read();
            var exportedConfig = Path.Combine(workFolder, ConfigEntry);
            File.WriteAllText(exportedConfig, JsonSerializer.Serialize(new AppConfig
            {
                SelectedTheme = config.SelectedTheme,
                SelectedLanguage = config.SelectedLanguage
            }, JsonOptions));
            files.Add((ConfigEntry, exportedConfig));

            var entries = new List<BackupFileEntry>(files.Count);
            using (var archiveStream = new FileStream(tempArchive, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var (entryPath, sourcePath) in files)
                        entries.Add(AddFile(archive, entryPath, sourcePath));

                    var manifest = new BackupManifest
                    {
                        AppVersion = GetAppVersion(),
                        CreatedAtUtc = DateTime.UtcNow,
                        LastMigration = lastMigration,
                        Files = entries
                    };

                    var manifestEntry = archive.CreateEntry(ManifestPath, CompressionLevel.Optimal);
                    using (var manifestStream = manifestEntry.Open())
                        JsonSerializer.Serialize(manifestStream, manifest, JsonOptions);

                    archiveStream.Flush();
                }

                archiveStream.Flush(flushToDisk: true);
            }

            File.Move(tempArchive, destinationPath, overwrite: true);
            return Validate(destinationPath);
        }
        catch (Exception ex)
        {
            return ResultT<BackupManifest>.Failure(Localizer.Format("Backup_Errors_UnableToExport", ex.Message));
        }
        finally
        {
            TryDeleteFile(tempArchive);
            TryDeleteDirectory(workFolder);
        }
    }

    public ResultT<BackupManifest> Validate(string archivePath)
    {
        var workFolder = CreateWorkFolder();
        try
        {
            return ExtractVerified(archivePath, workFolder);
        }
        finally
        {
            TryDeleteDirectory(workFolder);
        }
    }

    public ResultT<BackupManifest> StageRestore(string archivePath)
    {
        try
        {
            TryDeleteDirectory(paths.PendingRestoreFolder);
            var contentFolder = Path.Combine(paths.PendingRestoreFolder, ContentFolder);
            Directory.CreateDirectory(contentFolder);

            var result = ExtractVerified(archivePath, contentFolder);
            if (result.Failed)
            {
                TryDeleteDirectory(paths.PendingRestoreFolder);
                return result;
            }

            AtomicFile.WriteAllText(
                Path.Combine(paths.PendingRestoreFolder, ReadyMarker),
                JsonSerializer.Serialize(result.Value, JsonOptions));
            return result;
        }
        catch (Exception ex)
        {
            TryDeleteDirectory(paths.PendingRestoreFolder);
            return ResultT<BackupManifest>.Failure(Localizer.Format("Backup_Errors_UnableToImport", ex.Message));
        }
    }

    public void CancelPendingRestore() => TryDeleteDirectory(paths.PendingRestoreFolder);

    /// <summary>
    /// Creates a backup of the current data in the backups folder and keeps only the most recent ones for this reason.
    /// </summary>
    public ResultT<BackupManifest> CreateAutomaticBackup(string reason)
    {
        Directory.CreateDirectory(paths.BackupsFolder);
        var destination = Path.Combine(
            paths.BackupsFolder,
            $"{reason}-{DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)}{FileExtension}");

        var result = Export(destination);
        if (result.Succeeded)
            PruneAutomaticBackups(reason);

        return result;
    }

    /// <summary>
    /// Swaps the staged restore in place of the current data. Must run before any database connection is opened.
    /// Returns null when no restore is pending.
    /// </summary>
    public ResultT<RestoreTransaction>? BeginPendingRestore()
    {
        if (!HasPendingRestore)
            return null;

        var contentFolder = Path.Combine(paths.PendingRestoreFolder, ContentFolder);
        var previousFolder = Path.Combine(paths.RootPath, $"restore-previous-{DateTime.Now:yyyyMMddHHmmss}");
        try
        {
            // Re-check the staged content: it sat on disk between staging and this start.
            var stagedDatabase = Path.Combine(contentFolder, DatabaseEntry);
            if (File.Exists(stagedDatabase) && DatabaseHealth.Check(stagedDatabase) != DatabaseState.Healthy)
            {
                CancelPendingRestore();
                return ResultT<RestoreTransaction>.Failure(Localizer.Get("Backup_Errors_DatabaseCorrupt"));
            }

            var currentDatabase = DatabaseHealth.Check(paths.DatabasePath);
            if (currentDatabase == DatabaseState.Healthy)
            {
                var safety = CreateAutomaticBackup(PreImportReason);
                if (safety.Failed)
                    return ResultT<RestoreTransaction>.Failure(Localizer.Format("Backup_Errors_UnableToCreateSafetyBackup", safety.Error ?? string.Empty));
            }

            Directory.CreateDirectory(previousFolder);
            foreach (var file in DatabaseFiles(paths.DatabasePath))
                MoveIfExists(file, Path.Combine(previousFolder, Path.GetFileName(file)));
            MoveIfExists(paths.DocumentsFolder, Path.Combine(previousFolder, "documents"));
            MoveIfExists(paths.ThemesFolder, Path.Combine(previousFolder, "themes"));

            MoveIfExists(stagedDatabase, paths.DatabasePath);
            MoveIfExists(Path.Combine(contentFolder, "documents"), paths.DocumentsFolder);
            MoveIfExists(Path.Combine(contentFolder, "themes"), paths.ThemesFolder);
            Directory.CreateDirectory(paths.DocumentsFolder);
            Directory.CreateDirectory(paths.ThemesFolder);

            var previousConfig = configStore.Read();
            var stagedConfig = Path.Combine(contentFolder, ConfigEntry);
            if (File.Exists(stagedConfig))
            {
                var restored = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(stagedConfig), JsonOptions);
                configStore.Update(config =>
                {
                    config.SelectedTheme = restored?.SelectedTheme;
                    config.SelectedLanguage = restored?.SelectedLanguage ?? config.SelectedLanguage;
                });
            }

            return ResultT<RestoreTransaction>.Ok(new RestoreTransaction(this, previousFolder, previousConfig));
        }
        catch (Exception ex)
        {
            RollbackRestore(previousFolder, null);
            return ResultT<RestoreTransaction>.Failure(Localizer.Format("Backup_Errors_UnableToImport", ex.Message));
        }
    }

    internal void CommitRestore(string previousFolder)
    {
        TryDeleteDirectory(previousFolder);
        CancelPendingRestore();
    }

    internal void RollbackRestore(string previousFolder, AppConfig? previousConfig)
    {
        if (!Directory.Exists(previousFolder))
            return;

        foreach (var file in DatabaseFiles(paths.DatabasePath))
            TryDeleteFile(file);
        foreach (var file in Directory.EnumerateFiles(previousFolder))
            File.Move(file, Path.Combine(paths.RootPath, Path.GetFileName(file)), overwrite: true);

        RestoreFolder(Path.Combine(previousFolder, "documents"), paths.DocumentsFolder);
        RestoreFolder(Path.Combine(previousFolder, "themes"), paths.ThemesFolder);

        if (previousConfig is not null)
        {
            configStore.Update(config =>
            {
                config.SelectedTheme = previousConfig.SelectedTheme;
                config.SelectedLanguage = previousConfig.SelectedLanguage;
            });
        }

        TryDeleteDirectory(previousFolder);
        CancelPendingRestore();
    }

    private ResultT<BackupManifest> ExtractVerified(string archivePath, string destinationFolder)
    {
        if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
            return ResultT<BackupManifest>.NotFound(Localizer.Get("Backup_Errors_ArchiveNotFound"));

        try
        {
            using var archive = ZipFile.OpenRead(archivePath);
            if (archive.Entries.Count > MaxEntries)
                return Invalid("too many entries");

            var manifestEntry = archive.GetEntry(ManifestPath);
            if (manifestEntry is null || manifestEntry.Length > MaxManifestSize)
                return Invalid("missing manifest");

            BackupManifest? manifest;
            using (var manifestStream = manifestEntry.Open())
                manifest = JsonSerializer.Deserialize<BackupManifest>(manifestStream, JsonOptions);

            if (manifest is null)
                return Invalid("empty manifest");
            if (manifest.FormatVersion > BackupManifest.CurrentFormatVersion || manifest.FormatVersion < 1)
                return ResultT<BackupManifest>.Failure(Localizer.Get("Backup_Errors_UnsupportedFormat"));
            if (!LoomaMigrations.IsKnown(manifest.LastMigration))
                return ResultT<BackupManifest>.Failure(Localizer.Get("Backup_Errors_NewerVersion"));
            if (manifest.TotalSize > MaxTotalSize || manifest.Files.Any(file => file.Size < 0))
                return Invalid("declared size");

            var declared = new Dictionary<string, BackupFileEntry>(StringComparer.Ordinal);
            foreach (var file in manifest.Files)
            {
                if (!IsAllowedEntry(file.Path) || !declared.TryAdd(file.Path, file))
                    return Invalid($"unexpected path {file.Path}");
            }

            var destinationRoot = Path.GetFullPath(destinationFolder) + Path.DirectorySeparatorChar;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName == ManifestPath)
                    continue;
                if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith('/'))
                    continue; // directory entry

                if (!declared.TryGetValue(entry.FullName, out var expected) || !seen.Add(entry.FullName))
                    return Invalid($"undeclared entry {entry.FullName}");

                // Zip-slip guard, on top of the path whitelist.
                var target = Path.GetFullPath(Path.Combine(destinationFolder, entry.FullName));
                if (!target.StartsWith(destinationRoot, StringComparison.Ordinal))
                    return Invalid($"unsafe path {entry.FullName}");

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (!CopyVerified(entry, target, expected))
                    return ResultT<BackupManifest>.Failure(Localizer.Format("Backup_Errors_ChecksumMismatch", entry.FullName));
            }

            if (seen.Count != declared.Count)
                return Invalid("missing files");

            var database = Path.Combine(destinationFolder, DatabaseEntry);
            if (File.Exists(database))
            {
                if (DatabaseHealth.Check(database) != DatabaseState.Healthy)
                    return ResultT<BackupManifest>.Failure(Localizer.Get("Backup_Errors_DatabaseCorrupt"));
                if (!LoomaMigrations.IsKnown(LoomaMigrations.ReadLastApplied(database)))
                    return ResultT<BackupManifest>.Failure(Localizer.Get("Backup_Errors_NewerVersion"));
            }

            var config = Path.Combine(destinationFolder, ConfigEntry);
            if (File.Exists(config) && !AppConfigStore.IsValidJson(config))
                return Invalid("config.json");

            return ResultT<BackupManifest>.Ok(manifest);
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Invalid(ex.Message);
        }
    }

    private static ResultT<BackupManifest> Invalid(string detail) =>
        ResultT<BackupManifest>.Failure(Localizer.Format("Backup_Errors_InvalidArchive", detail));

    private static bool CopyVerified(ZipArchiveEntry entry, string target, BackupFileEntry expected)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using (var source = entry.Open())
        using (var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                // Never trust the declared sizes: stop as soon as the content exceeds them.
                if (total > expected.Size)
                    return false;

                hash.AppendData(buffer, 0, read);
                output.Write(buffer, 0, read);
            }

            if (total != expected.Size)
                return false;
        }

        return string.Equals(Convert.ToHexStringLower(hash.GetHashAndReset()), expected.Sha256, StringComparison.OrdinalIgnoreCase);
    }

    private static BackupFileEntry AddFile(ZipArchive archive, string entryPath, string sourcePath)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
        long size = 0;
        using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var destination = entry.Open())
        {
            var buffer = new byte[81920];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                destination.Write(buffer, 0, read);
                size += read;
            }
        }

        return new BackupFileEntry(entryPath, size, Convert.ToHexStringLower(hash.GetHashAndReset()));
    }

    internal static bool IsAllowedEntry(string path) =>
        path is DatabaseEntry or ConfigEntry
        || DocumentEntryRegex().IsMatch(path)
        || (ThemeEntryRegex().IsMatch(path) && !path.Contains("..", StringComparison.Ordinal));

    [GeneratedRegex(@"^documents/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}(\.[A-Za-z0-9_\-]{1,32})?$")]
    private static partial Regex DocumentEntryRegex();

    [GeneratedRegex(@"^themes/[A-Za-z0-9 _\-\.()]{1,128}\.json$")]
    private static partial Regex ThemeEntryRegex();

    private void PruneAutomaticBackups(string reason)
    {
        var obsolete = ListAutomaticBackups()
            .Where(backup => backup.Reason == reason)
            .Skip(AutomaticBackupsKept);

        foreach (var backup in obsolete)
            TryDeleteFile(backup.Path);
    }

    private static BackupInfo ToBackupInfo(string path)
    {
        var info = new FileInfo(path);
        var name = Path.GetFileNameWithoutExtension(path);
        var reason = name.StartsWith(PreMigrationReason, StringComparison.Ordinal) ? PreMigrationReason
            : name.StartsWith(PreImportReason, StringComparison.Ordinal) ? PreImportReason
            : name.StartsWith(PreResetReason, StringComparison.Ordinal) ? PreResetReason
            : name;
        return new BackupInfo(path, reason, info.LastWriteTime, info.Length);
    }

    private static IEnumerable<string> DatabaseFiles(string databasePath) =>
        [databasePath, $"{databasePath}-wal", $"{databasePath}-shm", $"{databasePath}-journal"];

    private static void MoveIfExists(string source, string destination)
    {
        if (File.Exists(source))
            File.Move(source, destination, overwrite: true);
        else if (Directory.Exists(source))
            Directory.Move(source, destination);
    }

    private static void RestoreFolder(string saved, string destination)
    {
        if (!Directory.Exists(saved))
            return;

        TryDeleteDirectory(destination);
        Directory.Move(saved, destination);
    }

    private string CreateWorkFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "looma-backup", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string? GetAppVersion() =>
        (Assembly.GetEntryAssembly() ?? typeof(BackupService).Assembly)
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort cleanup of temporary files.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort cleanup of temporary folders.
        }
    }
}

/// <summary>
/// A restore swapped in place at startup. Commit once the database opened and migrated,
/// otherwise roll back to the previous data.
/// </summary>
public sealed class RestoreTransaction
{
    private readonly BackupService _owner;
    private readonly AppConfig _previousConfig;
    private bool _completed;

    internal RestoreTransaction(BackupService owner, string previousFolder, AppConfig previousConfig)
    {
        _owner = owner;
        PreviousFolder = previousFolder;
        _previousConfig = previousConfig;
    }

    public string PreviousFolder { get; }

    public void Commit()
    {
        if (_completed)
            return;
        _owner.CommitRestore(PreviousFolder);
        _completed = true;
    }

    public void Rollback()
    {
        if (_completed)
            return;
        _owner.RollbackRestore(PreviousFolder, _previousConfig);
        _completed = true;
    }
}
