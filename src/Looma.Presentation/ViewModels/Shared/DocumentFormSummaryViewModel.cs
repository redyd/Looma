// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Looma.Presentation.ViewModels.Shared;

public partial class DocumentFormSummaryViewModel(
    Guid documentId,
    string nickname,
    string typeDisplay,
    string sizeDisplay,
    Action<DocumentFormSummaryViewModel>? removeRequested = null)
    : ObservableObject
{
    public Guid DocumentId { get; } = documentId;
    public string TypeDisplay { get; } = typeDisplay;
    public string SizeDisplay { get; } = sizeDisplay;
    public string OriginalNickname { get; } = nickname;

    [ObservableProperty]
    public partial string Nickname { get; set; } = nickname;

    public string DetailText => $"{TypeDisplay} · {SizeDisplay}";

    [RelayCommand]
    private void Remove() => removeRequested?.Invoke(this);
}
