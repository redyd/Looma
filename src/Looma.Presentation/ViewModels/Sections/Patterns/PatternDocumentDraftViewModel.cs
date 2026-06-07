// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternDocumentDraftViewModel(
    IDocumentFilePicker filePicker,
    Action<PatternDocumentDraftViewModel>? removeRequested = null)
    : ObservableObject
{
    [ObservableProperty]
    private string? _sourcePath;

    [ObservableProperty]
    private string _nickname = string.Empty;

    public string SelectedFileName =>
        string.IsNullOrWhiteSpace(SourcePath) ? "Aucun fichier sélectionné" : Path.GetFileName(SourcePath);

    public string SelectedFileDirectory =>
        string.IsNullOrWhiteSpace(SourcePath) ? string.Empty : Path.GetDirectoryName(SourcePath) ?? string.Empty;

    partial void OnSourcePathChanged(string? value)
    {
        OnPropertyChanged(nameof(SelectedFileName));
        OnPropertyChanged(nameof(SelectedFileDirectory));
    }

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        var path = await filePicker.PickDocumentAsync();
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
