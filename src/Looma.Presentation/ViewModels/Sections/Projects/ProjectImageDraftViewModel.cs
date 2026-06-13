// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectImageDraftViewModel(
    string sourcePath,
    Action<ProjectImageDraftViewModel>? removeRequested = null)
    : ObservableObject
{
    [ObservableProperty]
    private string _sourcePath = sourcePath;

    [ObservableProperty]
    private string _nickname = Path.GetFileNameWithoutExtension(sourcePath);

    public string SelectedFileName => Path.GetFileName(SourcePath);
    public string SelectedFileDirectory => Path.GetDirectoryName(SourcePath) ?? string.Empty;

    partial void OnSourcePathChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedFileName));
        OnPropertyChanged(nameof(SelectedFileDirectory));
    }

    [RelayCommand]
    private void Remove() => removeRequested?.Invoke(this);
}
