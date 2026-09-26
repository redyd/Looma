// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia.Controls;
using Avalonia.Media;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Looma.Views.UserControls.Statistics;

/// <summary>Convertit les brushes du thème courant en peintures LiveCharts.</summary>
internal static class ChartPaints
{
    public static SKColor Color(Control owner, string resourceKey)
    {
        var color = owner.TryFindResource(resourceKey, owner.ActualThemeVariant, out var resource)
                    && resource is ISolidColorBrush brush
            ? brush.Color
            : Colors.Gray;

        return new SKColor(color.R, color.G, color.B, color.A);
    }

    public static SolidColorPaint Paint(Control owner, string resourceKey, float strokeThickness = 1) =>
        new(Color(owner, resourceKey)) { StrokeThickness = strokeThickness };
}
