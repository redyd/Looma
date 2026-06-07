// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Looma.Presentation.Services;

namespace Looma.App.Services;

public sealed class AvaloniaDocumentFilePicker : IDocumentFilePicker
{
    public async Task<string?> PickDocumentAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        var window = desktop.MainWindow;
        if (window is null)
            return null;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionner un document",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Tous les fichiers")
                {
                    Patterns = ["*"]
                }
            ]
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }
}
