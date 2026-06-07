// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Documents;

public record DocumentSummaryViewModel(
    Document Document,
    ICommand OpenCommand,
    ICommand EditCommand,
    ICommand DeleteCommand)
{
    public string TypeDisplay => Document.Type;
    public string SizeDisplay => FormatSize(Document.SizeBytes);

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} {units[unitIndex]}"
            : $"{size:0.##} {units[unitIndex]}";
    }
}
