// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;
using Looma.Domain.Integrity;
using Looma.Domain.Request;
using Looma.Infrastructure.Entity;
using Looma.Infrastructure.Repositories;
using Looma.Infrastructure.Services;
using Looma.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Tests;

/// <summary>Uses a real database file, as the application does.</summary>
public sealed class BackupAndIntegrityTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "looma-backup-tests", Guid.NewGuid().ToString("N"));
    private readonly AppPaths _paths;
    private readonly AppConfigStore _config;
    private readonly BackupService _backup;

    public BackupAndIntegrityTests()
    {
        _paths = new AppPaths(_root);
        _paths.EnsureDirectoriesExist();
        _config = new AppConfigStore(_paths);
        _backup = new BackupService(_paths, _config);
        using var context = CreateContext();
        context.Database.Migrate();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Export_then_restore_brings_back_identical_data()
    {
        var documentId = await SeedAsync("Merino");
        _config.Update(config => config.SelectedLanguage = "nl");
        var archive = Path.Combine(_root, "export.looma");

        var export = _backup.Export(archive);
        export.Succeeded.Should().BeTrue(export.Error);
        export.Value!.DocumentCount.Should().Be(1);
        export.Value.LastMigration.Should().Be(LoomaMigrations.Known[^1]);

        // Diverge from the backup, then restore it.
        await using (var context = CreateContext())
        {
            context.Wools.Add(NewWool("Added after export"));
            await context.SaveChangesAsync();
        }
        File.Delete(_paths.GetDocumentStoragePath(documentId));
        _config.Update(config => config.SelectedLanguage = "de");

        _backup.StageRestore(archive).Succeeded.Should().BeTrue();
        SqliteConnection.ClearAllPools();
        var restore = _backup.BeginPendingRestore();
        restore.Should().NotBeNull();
        restore!.Succeeded.Should().BeTrue(restore.Error);
        restore.Value!.Commit();

        await using var verify = CreateContext();
        verify.Wools.Select(w => w.Name).Should().Equal("Merino");
        File.ReadAllText(_paths.GetDocumentStoragePath(documentId)).Should().Be("pattern content");
        _config.Read().SelectedLanguage.Should().Be("nl");
        _backup.ListAutomaticBackups().Should().ContainSingle(b => b.Reason == BackupService.PreImportReason);
        _backup.HasPendingRestore.Should().BeFalse();
        Directory.EnumerateDirectories(_root, "restore-previous-*").Should().BeEmpty();
    }

    [Fact]
    public async Task Rollback_puts_previous_data_back()
    {
        await SeedAsync("Original");
        var archive = Path.Combine(_root, "export.looma");
        _backup.Export(archive).Succeeded.Should().BeTrue();
        await using (var context = CreateContext())
        {
            context.Wools.Add(NewWool("Current"));
            await context.SaveChangesAsync();
        }

        _backup.StageRestore(archive);
        SqliteConnection.ClearAllPools();
        var restore = _backup.BeginPendingRestore()!.Value!;
        SqliteConnection.ClearAllPools();
        restore.Rollback();

        await using var verify = CreateContext();
        verify.Wools.Select(w => w.Name).Should().BeEquivalentTo("Original", "Current");
        _backup.HasPendingRestore.Should().BeFalse();
    }

    [Fact]
    public void Validate_rejects_tampered_file()
    {
        var archive = ExportWithDocument();
        RewriteEntry(archive, entry => entry.StartsWith("documents/"), "tampered");

        var result = _backup.Validate(archive);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_rejects_path_traversal_entries()
    {
        var archive = ExportWithDocument();
        using (var zip = ZipFile.Open(archive, ZipArchiveMode.Update))
        {
            var entry = zip.CreateEntry("../evil.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("evil");
        }

        var result = _backup.StageRestore(archive);

        result.Failed.Should().BeTrue();
        File.Exists(Path.Combine(_root, "evil.txt")).Should().BeFalse();
        File.Exists(Path.Combine(_paths.PendingRestoreFolder, "evil.txt")).Should().BeFalse();
        _backup.HasPendingRestore.Should().BeFalse();
    }

    [Fact]
    public void Validate_rejects_manifest_declaring_traversal_path()
    {
        var archive = ExportWithDocument();
        RewriteManifest(archive, manifest => manifest.Replace("\"config.json\"", "\"../config.json\""));

        _backup.Validate(archive).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_rejects_backup_from_newer_version()
    {
        var archive = ExportWithDocument();
        RewriteManifest(archive, manifest => manifest.Replace(LoomaMigrations.Known[^1], "29990101000000_Future"));

        _backup.Validate(archive).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_rejects_non_archive_file()
    {
        var path = Path.Combine(_root, "not-a-zip.looma");
        File.WriteAllText(path, "hello");

        _backup.Validate(path).Failed.Should().BeTrue();
    }

    [Fact]
    public void AutomaticBackups_are_rotated()
    {
        for (var i = 0; i < BackupService.AutomaticBackupsKept + 2; i++)
            _backup.CreateAutomaticBackup(BackupService.PreMigrationReason).Succeeded.Should().BeTrue();

        _backup.ListAutomaticBackups().Should().HaveCount(BackupService.AutomaticBackupsKept);
    }

    [Fact]
    public void DatabaseHealth_detects_corrupted_and_missing_files()
    {
        SqliteConnection.ClearAllPools();
        DatabaseHealth.Check(_paths.DatabasePath).Should().Be(DatabaseState.Healthy);

        var garbage = Path.Combine(_root, "garbage.db");
        File.WriteAllBytes(garbage, Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray());
        DatabaseHealth.Check(garbage).Should().Be(DatabaseState.Corrupt);

        var truncated = Path.Combine(_root, "truncated.db");
        var bytes = File.ReadAllBytes(_paths.DatabasePath);
        File.WriteAllBytes(truncated, bytes.Take(bytes.Length / 3).ToArray());
        DatabaseHealth.Check(truncated).Should().NotBe(DatabaseState.Healthy);

        DatabaseHealth.Check(Path.Combine(_root, "absent.db")).Should().Be(DatabaseState.Missing);
    }

    [Fact]
    public async Task Integrity_reports_missing_and_orphan_files_and_repairs_them_safely()
    {
        var documentId = await SeedAsync("Wool");
        File.Delete(_paths.GetDocumentStoragePath(documentId));
        var orphan = Path.Combine(_paths.DocumentsFolder, $"{Guid.NewGuid()}.pdf");
        File.WriteAllText(orphan, "orphan");
        File.WriteAllText(Path.Combine(_paths.ThemesFolder, "broken.json"), "{");
        var service = new DataIntegrityService(CreateContext, _paths, _config);

        var report = (await service.CheckAsync()).Value!;

        report.Of(IntegrityIssueKind.DocumentFileMissing).Should().ContainSingle(i => i.Target == documentId.ToString());
        report.Of(IntegrityIssueKind.DocumentFileOrphan).Should().ContainSingle();
        report.Of(IntegrityIssueKind.ThemeInvalid).Should().ContainSingle(i => i.Target == "broken.json");

        (await service.QuarantineOrphanFilesAsync()).Value.Should().Be(1);
        File.Exists(orphan).Should().BeFalse();
        File.Exists(Path.Combine(_paths.OrphanDocumentsFolder, Path.GetFileName(orphan))).Should().BeTrue();

        (await service.RemoveMissingDocumentEntriesAsync()).Value.Should().Be(1);
        var after = (await service.CheckAsync()).Value!;
        after.Of(IntegrityIssueKind.DocumentFileMissing).Should().BeEmpty();
        after.Of(IntegrityIssueKind.DocumentFileOrphan).Should().BeEmpty();
    }

    [Fact]
    public async Task Reset_backs_up_then_erases_all_data()
    {
        var documentId = await SeedAsync("Wool");
        File.WriteAllText(Path.Combine(_paths.ThemesFolder, "mine.json"), "{}");
        _config.Update(config => { config.SelectedLanguage = "de"; config.Version = "1.2.3"; });

        (await _backup.ScheduleResetAsync()).Succeeded.Should().BeTrue();
        SqliteConnection.ClearAllPools();
        var result = _backup.ApplyPendingReset();

        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue(result.Error);
        File.Exists(_paths.DatabasePath).Should().BeFalse();
        File.Exists(_paths.GetDocumentStoragePath(documentId)).Should().BeFalse();
        Directory.EnumerateFiles(_paths.ThemesFolder).Should().BeEmpty();
        _config.Read().SelectedLanguage.Should().BeNull();
        _config.Read().Version.Should().Be("1.2.3", "release-note tracking is not user data");
        _backup.HasPendingReset.Should().BeFalse();

        var backup = _backup.ListAutomaticBackups().Should().ContainSingle(b => b.Reason == BackupService.PreResetReason).Subject;
        var validation = _backup.Validate(backup.Path);
        validation.Succeeded.Should().BeTrue(validation.Error);
        validation.Value!.DocumentCount.Should().Be(1);
    }

    [Fact]
    public void Reset_with_damaged_database_moves_files_aside_instead_of_deleting()
    {
        SqliteConnection.ClearAllPools();
        File.WriteAllText(_paths.DatabasePath, "damaged database content, not sqlite at all.....");
        var document = Path.Combine(_paths.DocumentsFolder, $"{Guid.NewGuid()}.pdf");
        File.WriteAllText(document, "keep me");

        _backup.ScheduleResetAsync().GetAwaiter().GetResult();
        var result = _backup.ApplyPendingReset();

        result!.Succeeded.Should().BeTrue(result.Error);
        var previous = Directory.EnumerateDirectories(_root, "reset-previous-*").Should().ContainSingle().Subject;
        File.Exists(Path.Combine(previous, "looma.db")).Should().BeTrue();
        File.Exists(Path.Combine(previous, "documents", Path.GetFileName(document))).Should().BeTrue();
        File.Exists(_paths.DatabasePath).Should().BeFalse();
    }

    [Fact]
    public void ApplyPendingReset_without_request_does_nothing()
    {
        _backup.ApplyPendingReset().Should().BeNull();
        File.Exists(_paths.DatabasePath).Should().BeTrue();
    }

    [Fact]
    public async Task ScheduleReset_cancels_a_pending_import()
    {
        await SeedAsync("Wool");
        var archive = Path.Combine(_root, "export.looma");
        _backup.Export(archive).Succeeded.Should().BeTrue();
        _backup.StageRestore(archive).Succeeded.Should().BeTrue();

        await _backup.ScheduleResetAsync();

        _backup.HasPendingRestore.Should().BeFalse();
        _backup.HasPendingReset.Should().BeTrue();
    }

    private LoomaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<LoomaDbContext>()
            .UseSqlite($"Data Source={_paths.DatabasePath}")
            .Options);

    private static WoolEntity NewWool(string name) => new()
    {
        Name = name,
        Brand = "Brand",
        Material = "Merino",
        Color = "Blue",
        Weight = 100,
        Length = 200,
        Stock = 1000,
        NeedleMinSize = 4,
        NeedleMaxSize = 5
    };

    private async Task<Guid> SeedAsync(string woolName)
    {
        await using var context = CreateContext();
        context.Wools.Add(NewWool(woolName));
        await context.SaveChangesAsync();

        var source = Path.Combine(_root, "source.pdf");
        File.WriteAllText(source, "pattern content");
        var added = await new DocumentRepository(context, _paths).AddAsync(new CreateDocumentRequest(source, "Doc"));
        return added.Value!.Id;
    }

    private string ExportWithDocument()
    {
        SeedAsync("Wool").GetAwaiter().GetResult();
        var archive = Path.Combine(_root, "export.looma");
        _backup.Export(archive).Succeeded.Should().BeTrue();
        return archive;
    }

    private static void RewriteEntry(string archive, Func<string, bool> match, string content)
    {
        using var zip = ZipFile.Open(archive, ZipArchiveMode.Update);
        var entry = zip.Entries.First(e => match(e.FullName));
        var name = entry.FullName;
        entry.Delete();
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write(content);
    }

    private static void RewriteManifest(string archive, Func<string, string> rewrite)
    {
        string manifest;
        using (var zip = ZipFile.OpenRead(archive))
        using (var reader = new StreamReader(zip.GetEntry("manifest.json")!.Open()))
            manifest = reader.ReadToEnd();

        JsonDocument.Parse(manifest).Dispose();
        RewriteEntry(archive, name => name == "manifest.json", rewrite(manifest));
    }
}
