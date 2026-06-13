// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Looma.Presentation.Services;

namespace Looma.App.Services;

public sealed class AvaloniaDocumentFilePicker : IDocumentFilePicker
{
    public Task<string?> PickDocumentAsync() =>
        PickFileAsync("Sélectionner un document", false,
        [
            new FilePickerFileType("Tous les fichiers")
            {
                Patterns = ["*"]
            }
        ]);

    public Task<string?> PickImageAsync() =>
        PickFileAsync("Sélectionner une image", false, ImageFileTypes);

    public async Task<IReadOnlyList<string>> PickImagesAsync()
    {
        var paths = await PickFilesAsync("Sélectionner des images", true, ImageFileTypes);
        return paths;
    }

    private static IReadOnlyList<FilePickerFileType> ImageFileTypes =>
    [
        new FilePickerFileType("Images")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp", "*.gif"],
            MimeTypes = ["image/png", "image/jpeg", "image/webp", "image/bmp", "image/gif"]
        },
        new FilePickerFileType("Tous les fichiers")
        {
            Patterns = ["*"]
        }
    ];

    private static async Task<string?> PickFileAsync(
        string title,
        bool allowMultiple,
        IReadOnlyList<FilePickerFileType> fileTypes)
    {
        var paths = await PickFilesAsync(title, allowMultiple, fileTypes);
        return paths.FirstOrDefault();
    }

    private static async Task<IReadOnlyList<string>> PickFilesAsync(
        string title,
        bool allowMultiple,
        IReadOnlyList<FilePickerFileType> fileTypes)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return [];

        var window = desktop.MainWindow;
        if (window is null)
            return [];

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = fileTypes
        });

        return files
            .Select(file => file.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .ToList();
    }
}
