// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Backup;
using Looma.Domain.Integrity;
using Looma.Domain.IServices;
using Looma.Domain.Refresh;
using Looma.Presentation.Notifications;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Settings;

/// <summary>Settings section for backups (export / import) and data integrity.</summary>
public partial class SettingsDataViewModel(
    IBackupService backupService,
    IDataIntegrityService integrityService,
    IBackupFilePicker filePicker,
    IAppLifetimeService lifetime,
    INotificationService notifications,
    IDataRefreshService? refreshService = null)
    : ViewModelBase
{
    private string? _pendingImportPath;

    public ObservableCollection<string> IntegrityIssues { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(ExportCommand), nameof(PickImportCommand), nameof(ConfirmImportCommand),
        nameof(CheckIntegrityCommand), nameof(QuarantineOrphansCommand), nameof(RemoveMissingDocumentsCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsImportConfirmationVisible { get; set; }

    [ObservableProperty]
    public partial string? ImportSummary { get; set; }

    [ObservableProperty]
    public partial bool IntegrityChecked { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHealthy))]
    public partial bool HasIntegrityIssues { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(QuarantineOrphansCommand))]
    public partial bool HasOrphanFiles { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveMissingDocumentsCommand))]
    public partial bool HasMissingDocuments { get; set; }

    public bool IsHealthy => IntegrityChecked && !HasIntegrityIssues;

    private bool CanAct() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task ExportAsync()
    {
        var destination = await filePicker.PickArchiveSaveLocationAsync($"looma-backup-{DateTime.Now:yyyyMMdd-HHmmss}.looma");
        if (string.IsNullOrWhiteSpace(destination))
            return;

        await RunBusyAsync(async () =>
        {
            var result = await backupService.ExportAsync(destination);
            if (result.Failed)
            {
                notifications.Error(result.Error ?? Translation["Settings_Data_Notifications_UnableToExport"]);
                return;
            }

            notifications.Success(Translation.Format("Settings_Data_Notifications_Exported", destination));
        });
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task PickImportAsync()
    {
        var path = await filePicker.PickArchiveToOpenAsync();
        if (string.IsNullOrWhiteSpace(path))
            return;

        await RunBusyAsync(async () =>
        {
            var result = await backupService.ValidateAsync(path);
            if (result.Failed || result.Value is null)
            {
                notifications.Error(result.Error ?? Translation["Settings_Data_Notifications_InvalidArchive"]);
                return;
            }

            _pendingImportPath = path;
            ImportSummary = DescribeManifest(result.Value);
            IsImportConfirmationVisible = true;
        });
    }

    [RelayCommand]
    private void CancelImport()
    {
        _pendingImportPath = null;
        ImportSummary = null;
        IsImportConfirmationVisible = false;
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task ConfirmImportAsync()
    {
        if (_pendingImportPath is null)
            return;

        var path = _pendingImportPath;
        var staged = false;
        await RunBusyAsync(async () =>
        {
            var result = await backupService.StageRestoreAsync(path);
            if (result.Failed)
            {
                notifications.Error(result.Error ?? Translation["Settings_Data_Notifications_InvalidArchive"]);
                return;
            }

            staged = true;
        });

        CancelImport();
        if (staged)
            lifetime.Restart();
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private Task CheckIntegrityAsync() => RunBusyAsync(RefreshIntegrityAsync);

    private bool CanQuarantineOrphans() => !IsBusy && HasOrphanFiles;

    [RelayCommand(CanExecute = nameof(CanQuarantineOrphans))]
    private Task QuarantineOrphansAsync() => RunBusyAsync(async () =>
    {
        var result = await integrityService.QuarantineOrphanFilesAsync();
        if (result.Failed)
            notifications.Error(result.Error ?? Translation["Settings_Data_Notifications_RepairFailed"]);
        else
            notifications.Success(Translation.Format("Settings_Data_Notifications_OrphansQuarantined", result.Value));

        await RefreshIntegrityAsync();
    });

    private bool CanRemoveMissingDocuments() => !IsBusy && HasMissingDocuments;

    [RelayCommand(CanExecute = nameof(CanRemoveMissingDocuments))]
    private Task RemoveMissingDocumentsAsync() => RunBusyAsync(async () =>
    {
        var result = await integrityService.RemoveMissingDocumentEntriesAsync();
        if (result.Failed)
        {
            notifications.Error(result.Error ?? Translation["Settings_Data_Notifications_RepairFailed"]);
        }
        else
        {
            notifications.Success(Translation.Format("Settings_Data_Notifications_MissingDocumentsRemoved", result.Value));
            refreshService?.RequestRefresh(RefreshScope.Documents | RefreshScope.Patterns | RefreshScope.Projects, "Missing documents removed.");
        }

        await RefreshIntegrityAsync();
    });

    [RelayCommand]
    private void OpenBackupsFolder()
    {
        try
        {
            Directory.CreateDirectory(backupService.BackupsFolder);
            lifetime.OpenFolder(backupService.BackupsFolder);
        }
        catch (Exception ex)
        {
            notifications.Error(Translation.Format("Common_UnexpectedErrorWithMessage", ex.Message));
        }
    }

    private async Task RefreshIntegrityAsync()
    {
        var result = await integrityService.CheckAsync();
        IntegrityIssues.Clear();
        if (result.Failed || result.Value is null)
        {
            IntegrityChecked = false;
            HasIntegrityIssues = false;
            HasOrphanFiles = false;
            HasMissingDocuments = false;
            notifications.Error(result.Error ?? Translation["Settings_Data_Notifications_RepairFailed"]);
            return;
        }

        var report = result.Value;
        foreach (var issue in report.Issues)
            IntegrityIssues.Add(DescribeIssue(issue));

        IntegrityChecked = true;
        HasIntegrityIssues = !report.IsHealthy;
        HasOrphanFiles = report.Of(IntegrityIssueKind.DocumentFileOrphan).Count > 0;
        HasMissingDocuments = report.Of(IntegrityIssueKind.DocumentFileMissing).Count > 0;
        OnPropertyChanged(nameof(IsHealthy));
    }

    private string DescribeIssue(IntegrityIssue issue) => issue.Kind switch
    {
        IntegrityIssueKind.DatabaseCorrupt => Translation.Format("Integrity_Issue_DatabaseCorrupt", issue.Detail ?? string.Empty),
        IntegrityIssueKind.ConfigRecovered => Translation.Format("Integrity_Issue_ConfigRecovered", issue.Target),
        IntegrityIssueKind.ThemeInvalid => Translation.Format("Integrity_Issue_ThemeInvalid", issue.Target),
        IntegrityIssueKind.DocumentFileMissing => Translation.Format("Integrity_Issue_DocumentFileMissing", issue.Detail ?? issue.Target),
        IntegrityIssueKind.DocumentFileOrphan => Translation.Format("Integrity_Issue_DocumentFileOrphan", issue.Target),
        _ => issue.Target
    };

    private string DescribeManifest(BackupManifest manifest) =>
        Translation.Format(
            "Settings_Data_ImportSummary",
            manifest.CreatedAtUtc.ToLocalTime().ToString("g"),
            manifest.AppVersion ?? "?",
            manifest.DocumentCount,
            manifest.ThemeCount);

    private async Task RunBusyAsync(Func<Task> action)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            notifications.Error(Translation.Format("Common_UnexpectedErrorWithMessage", ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
