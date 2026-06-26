// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Shared.Documents;

public partial class DocumentDraftViewModel(IDocumentFilePicker filePicker, DocumentPickerMode mode, Action<DocumentDraftViewModel>? removeRequested = null)
    : ObservableObject
{
    [ObservableProperty]
    public partial string? SourcePath { get; set; }

    [ObservableProperty]
    public partial string Nickname { get; set; } = string.Empty;

    public string SelectedFileName
        => string.IsNullOrWhiteSpace(SourcePath) ? TranslationService.Current["Documents_NoFileSelected"] : Path.GetFileName(SourcePath);

    public string SelectedFileDirectory
        => string.IsNullOrWhiteSpace(SourcePath) ? string.Empty : Path.GetDirectoryName(SourcePath) ?? string.Empty;

    partial void OnSourcePathChanged(string? value)
    {
        OnPropertyChanged(nameof(SelectedFileName));
        OnPropertyChanged(nameof(SelectedFileDirectory));
    }

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        var path = await filePicker.PickAsync(mode);
        if (string.IsNullOrWhiteSpace(path))
            return;

        SourcePath = path;
        if (string.IsNullOrWhiteSpace(Nickname))
            Nickname = Path.GetFileNameWithoutExtension(path);
    }

    [RelayCommand]
    private void Remove() => removeRequested?.Invoke(this);

    public void Reset()
    {
        SourcePath = null;
        Nickname = string.Empty;
    }
}
