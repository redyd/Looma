// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Looma.Presentation.ViewModels.Shared.Patterns;

public partial class PatternProjectViewModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "Aucun nom";
    [ObservableProperty] public partial string StatusDisplay { get; set; } = "Aucun status";
    public ICommand? OpenCommand { get; init; }
}
