// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Looma.Presentation.Services;

namespace Looma.App.Services;

public sealed class AvaloniaBackupFilePicker(TranslationService translation) : IBackupFilePicker
{
    private FilePickerFileType BackupFileType => new(translation["Backup_FileType"])
    {
        Patterns = ["*.looma", "*.zip"],
        MimeTypes = ["application/zip"]
    };

    public async Task<string?> PickArchiveToOpenAsync()
    {
        var window = GetWindow();
        if (window is null)
            return null;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = translation["Backup_PickerImportTitle"],
            AllowMultiple = false,
            FileTypeFilter = [BackupFileType]
        });

        return files
            .Select(file => file.TryGetLocalPath())
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
    }

    public async Task<string?> PickArchiveSaveLocationAsync(string suggestedFileName)
    {
        var window = GetWindow();
        if (window is null)
            return null;

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = translation["Backup_PickerExportTitle"],
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "looma",
            ShowOverwritePrompt = true,
            FileTypeChoices = [BackupFileType]
        });

        return file?.TryGetLocalPath();
    }

    private static TopLevel? GetWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(window => window.IsActive) ?? desktop.MainWindow
            : null;
}
