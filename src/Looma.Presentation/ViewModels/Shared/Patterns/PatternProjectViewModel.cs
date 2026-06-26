// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Shared.Patterns;

public partial class PatternProjectViewModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = TranslationService.Current["Common_NoName"];
    [ObservableProperty] public partial string StatusDisplay { get; set; } = TranslationService.Current["Common_NoStatus"];
    public ICommand? OpenCommand { get; init; }
}
