// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public partial class PatternExistingDocumentViewModel(
    Guid documentId,
    string nickname,
    string typeDisplay,
    string sizeDisplay,
    Action<PatternExistingDocumentViewModel>? removeRequested = null)
    : ObservableObject
{
    public Guid DocumentId { get; } = documentId;

    [ObservableProperty]
    private string _nickname = nickname;

    public string TypeDisplay { get; } = typeDisplay;
    public string SizeDisplay { get; } = sizeDisplay;
    public string DetailText => $"{TypeDisplay} · {SizeDisplay}";

    public string OriginalNickname { get; } = nickname;

    [RelayCommand]
    private void Remove() => removeRequested?.Invoke(this);
}
