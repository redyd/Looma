// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Backup;
using Looma.Domain.IServices;
using Looma.Presentation.Services;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Recovery;

/// <summary>
/// Shown instead of the main window when the data cannot be opened safely.
/// Every action leaves the damaged data in place (set aside, never deleted) and restarts the application.
/// </summary>
public partial class RecoveryViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;
    private readonly IBackupFilePicker _filePicker;
    private readonly IDatabaseRecovery _databaseRecovery;
    private readonly IAppLifetimeService _lifetime;

    public RecoveryViewModel(
        string problem,
        IBackupService backupService,
        IBackupFilePicker filePicker,
        IDatabaseRecovery databaseRecovery,
        IAppLifetimeService lifetime)
    {
        Problem = problem;
        _backupService = backupService;
        _filePicker = filePicker;
        _databaseRecovery = databaseRecovery;
        _lifetime = lifetime;

        foreach (var backup in backupService.ListAutomaticBackups())
            Backups.Add(new RecoveryBackupViewModel(backup, Translation));

        SelectedBackup = Backups.FirstOrDefault();
    }

    public string Problem { get; }

    public ObservableCollection<RecoveryBackupViewModel> Backups { get; } = [];

    public bool HasBackups => Backups.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestoreSelectedBackupCommand))]
    public partial RecoveryBackupViewModel? SelectedBackup { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestoreSelectedBackupCommand), nameof(ImportArchiveCommand), nameof(StartEmptyCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    private bool CanRestoreSelected() => SelectedBackup is not null && !IsBusy;

    private bool CanAct() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRestoreSelected))]
    private Task RestoreSelectedBackupAsync() => StageAndRestartAsync(SelectedBackup!.Backup.Path);

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task ImportArchiveAsync()
    {
        var path = await _filePicker.PickArchiveToOpenAsync();
        if (!string.IsNullOrWhiteSpace(path))
            await StageAndRestartAsync(path);
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private void StartEmpty()
    {
        var result = _databaseRecovery.SetAsideDatabase();
        if (result.Failed)
        {
            ErrorMessage = result.Error;
            return;
        }

        _lifetime.Restart();
    }

    [RelayCommand]
    private void OpenBackupsFolder() => _lifetime.OpenFolder(_backupService.BackupsFolder);

    [RelayCommand]
    private void Quit() => _lifetime.Quit();

    private async Task StageAndRestartAsync(string archivePath)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _backupService.StageRestoreAsync(archivePath);
            if (result.Failed)
            {
                ErrorMessage = result.Error;
                return;
            }

            // The corrupted database is replaced at the next start; keep a copy of it anyway.
            var setAside = _databaseRecovery.SetAsideDatabase();
            if (setAside.Failed)
            {
                ErrorMessage = setAside.Error;
                return;
            }

            _lifetime.Restart();
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed class RecoveryBackupViewModel(BackupInfo backup, TranslationService translation)
{
    public BackupInfo Backup { get; } = backup;

    public string Label => translation.Format("Recovery_BackupLabel", Backup.CreatedAt.ToString("g"), ReasonLabel);

    private string ReasonLabel => Backup.Reason switch
    {
        "pre-migration" => translation["Backup_Reason_PreMigration"],
        "pre-import" => translation["Backup_Reason_PreImport"],
        _ => Backup.Reason
    };
}
