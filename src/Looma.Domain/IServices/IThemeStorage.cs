// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Services;

public interface IThemeStorage
{
    IReadOnlyList<string> GetThemeFiles();
    string? GetSelectedThemePath();
    int SeedThemeFiles(string sourceFolder);
    void SaveSelectedTheme(string? themePath);
    void DeleteTheme(string themePath);
    string ImportTheme(string sourcePath);
    string CreateExportPath();
}
