// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Looma.Presentation.Services;
using System;
using Looma.Domain.Entities;

namespace Looma.App.Services;

public sealed class AvaloniaDocumentFilePicker : IDocumentFilePicker
{
    private static readonly List<string> ImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif"];
    public Task<string?> PickAsync(DocumentPickerMode mode) => mode switch
    {
        DocumentPickerMode.Images => PickFileAsync("Sélectionner une image", false, ImageFileTypes),
        DocumentPickerMode.All => PickFileAsync("Sélectionner un document", false, AllFileTypes),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    public async Task<List<string>> PicksAsync(DocumentPickerMode mode)
    {
        var paths = mode switch
        {
            DocumentPickerMode.Images => await PickFilesAsync("Sélectionner des images", true, ImageFileTypes),
            DocumentPickerMode.All => await PickFilesAsync("Sélectionner des documents", true, AllFileTypes),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        return [.. paths];
    }

    public bool IsSupportedFile(DocumentPickerMode mode, Document document)
    {
        if (document.StoragePath is null) return false;
        if (mode == DocumentPickerMode.All) return true;
        
        var fileExt = Path.GetExtension(document.StoragePath);
        var isImage = ImageExtensions.Any(ext => fileExt.Equals(ext, StringComparison.OrdinalIgnoreCase));
        
        return isImage;
    }

    private static IReadOnlyList<FilePickerFileType> AllFileTypes =>
    [
        new("Tous les fichiers") { Patterns = ["*"] }
    ];

    private static IReadOnlyList<FilePickerFileType> ImageFileTypes =>
    [
        new("Images")
        {
            Patterns = ImageExtensions.Select(ext => $"*{ext}").ToArray(),
            MimeTypes = ImageExtensions.Select(ext => $"image/{ext}").ToArray()
        },
        new("Tous les fichiers")
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

        return [.. files
            .Select(file => file.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)];
    }
}
