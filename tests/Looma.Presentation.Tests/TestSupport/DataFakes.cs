// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Backup;
using Looma.Domain.Core;
using Looma.Domain.Integrity;
using Looma.Domain.IServices;
using Looma.Presentation.Services;

namespace Looma.Presentation.Tests.TestSupport;

internal sealed class FakeBackupService : IBackupService
{
    public string BackupsFolder => "/backups";
    public ResultT<BackupManifest> ExportResult { get; set; } = ResultT<BackupManifest>.Ok(new BackupManifest());
    public ResultT<BackupManifest> ValidateResult { get; set; } = ResultT<BackupManifest>.Ok(new BackupManifest { AppVersion = "1.0.0" });
    public ResultT<BackupManifest> StageResult { get; set; } = ResultT<BackupManifest>.Ok(new BackupManifest());
    public List<string> Exported { get; } = [];
    public List<string> Staged { get; } = [];
    public List<BackupInfo> Backups { get; } = [];

    public Task<ResultT<BackupManifest>> ExportAsync(string destinationPath)
    {
        Exported.Add(destinationPath);
        return Task.FromResult(ExportResult);
    }

    public Task<ResultT<BackupManifest>> ValidateAsync(string archivePath) => Task.FromResult(ValidateResult);

    public Task<ResultT<BackupManifest>> StageRestoreAsync(string archivePath)
    {
        Staged.Add(archivePath);
        return Task.FromResult(StageResult);
    }

    public IReadOnlyList<BackupInfo> ListAutomaticBackups() => Backups;
}

internal sealed class FakeDataIntegrityService : IDataIntegrityService
{
    public IntegrityReport Report { get; set; } = IntegrityReport.Healthy;
    public int QuarantineCalls { get; private set; }
    public int RemoveCalls { get; private set; }

    public Task<ResultT<IntegrityReport>> CheckAsync() => Task.FromResult(ResultT<IntegrityReport>.Ok(Report));

    public Task<ResultT<int>> QuarantineOrphanFilesAsync()
    {
        QuarantineCalls++;
        Report = IntegrityReport.Healthy;
        return Task.FromResult(ResultT<int>.Ok(1));
    }

    public Task<ResultT<int>> RemoveMissingDocumentEntriesAsync()
    {
        RemoveCalls++;
        Report = IntegrityReport.Healthy;
        return Task.FromResult(ResultT<int>.Ok(1));
    }
}

internal sealed class FakeBackupFilePicker : IBackupFilePicker
{
    public string? OpenPath { get; set; } = "/tmp/backup.looma";
    public string? SavePath { get; set; } = "/tmp/export.looma";

    public Task<string?> PickArchiveToOpenAsync() => Task.FromResult(OpenPath);

    public Task<string?> PickArchiveSaveLocationAsync(string suggestedFileName) => Task.FromResult(SavePath);
}

internal sealed class FakeAppLifetimeService : IAppLifetimeService
{
    public int RestartCalls { get; private set; }
    public int QuitCalls { get; private set; }

    public void Restart() => RestartCalls++;

    public void Quit() => QuitCalls++;

    public void OpenFolder(string path)
    {
    }
}
