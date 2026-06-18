// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Presentation.ViewModels.Sections.Settings;

public sealed class ThemeOptionViewModel(string name, string? path)
{
    public string Name { get; } = name;
    public string? Path { get; } = path;
    public bool IsDefault => Path is null;
}
