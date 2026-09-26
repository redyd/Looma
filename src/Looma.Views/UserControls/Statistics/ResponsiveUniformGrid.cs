// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia;
using Avalonia.Controls.Primitives;

namespace Looma.Views.UserControls.Statistics;

/// <summary>
/// Grille à colonnes égales dont le nombre de colonnes s'adapte à la largeur disponible
/// (au moins <see cref="MinColumnWidth"/> par colonne, au plus <see cref="MaxColumns"/>),
/// en privilégiant des lignes complètes.
/// </summary>
public class ResponsiveUniformGrid : UniformGrid
{
    public static readonly StyledProperty<double> MinColumnWidthProperty =
        AvaloniaProperty.Register<ResponsiveUniformGrid, double>(nameof(MinColumnWidth), 240);

    public static readonly StyledProperty<int> MaxColumnsProperty =
        AvaloniaProperty.Register<ResponsiveUniformGrid, int>(nameof(MaxColumns), 4);

    static ResponsiveUniformGrid()
    {
        AffectsMeasure<ResponsiveUniformGrid>(MinColumnWidthProperty, MaxColumnsProperty);
    }

    public double MinColumnWidth
    {
        get => GetValue(MinColumnWidthProperty);
        set => SetValue(MinColumnWidthProperty, value);
    }

    public int MaxColumns
    {
        get => GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var visible = Children.Count(child => child.IsVisible);
        var fit = double.IsInfinity(availableSize.Width)
            ? MaxColumns
            : (int)((availableSize.Width + ColumnSpacing) / (MinColumnWidth + ColumnSpacing));
        var columns = Math.Clamp(fit, 1, Math.Max(1, Math.Min(MaxColumns, visible)));

        // Évite une dernière ligne incomplète (ex. 3 + 1) : on préfère des lignes pleines.
        while (columns > 1 && visible % columns != 0)
            columns--;
        if (Columns != columns)
            Columns = columns;

        return base.MeasureOverride(availableSize);
    }
}
