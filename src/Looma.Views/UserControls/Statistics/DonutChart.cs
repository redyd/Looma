// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using Looma.Presentation.ViewModels.Sections.Statistics;

namespace Looma.Views.UserControls.Statistics;

/// <summary>Donut avec total au centre et légende détaillée (libellé, valeur, pourcentage).</summary>
public sealed class DonutChart : UserControl
{
    public static readonly StyledProperty<StatisticsDonutChart?> ChartProperty =
        AvaloniaProperty.Register<DonutChart, StatisticsDonutChart?>(nameof(Chart));

    public static readonly StyledProperty<string?> EmptyTextProperty =
        AvaloniaProperty.Register<DonutChart, string?>(nameof(EmptyText));

    private const double PieSize = 150;

    // En dessous de cette largeur, la légende passe sous le donut pour garder les libellés lisibles.
    private const double StackedLayoutWidth = 400;

    private readonly PieChart _pie = new()
    {
        Width = PieSize,
        Height = PieSize,
        LegendPosition = LegendPosition.Hidden,
        TooltipPosition = TooltipPosition.Hidden
    };

    private readonly TextBlock _total = new()
    {
        Classes = { "stat-donut-total" },
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TextAlignment = TextAlignment.Center,
        MaxWidth = PieSize * 0.55,
        TextWrapping = TextWrapping.Wrap
    };

    private readonly StackPanel _legend = new() { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
    private readonly Grid _content;
    private readonly Grid _pieHost;
    private bool? _isStacked;

    private readonly TextBlock _empty = new()
    {
        Classes = { "empty-state-message" },
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TextWrapping = TextWrapping.Wrap,
        TextAlignment = TextAlignment.Center
    };

    public DonutChart()
    {
        _pieHost = new Grid
        {
            Width = PieSize,
            Height = PieSize,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _pie, _total }
        };
        Grid.SetColumn(_legend, 1);
        _content = new Grid
        {
            ColumnSpacing = 20,
            Children = { _pieHost, _legend }
        };
        Content = new Grid { MinHeight = PieSize, Children = { _content, _empty } };
        ActualThemeVariantChanged += (_, _) => Rebuild();
        SizeChanged += (_, args) => ApplyLayout(args.NewSize.Width < StackedLayoutWidth);
        ApplyLayout(false);
    }

    private void ApplyLayout(bool stacked)
    {
        if (_isStacked == stacked)
            return;

        _isStacked = stacked;
        _content.ColumnDefinitions = new ColumnDefinitions(stacked ? "*" : "Auto,*");
        _content.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto" : "Auto");
        _content.RowSpacing = stacked ? 16 : 0;
        _pieHost.HorizontalAlignment = stacked ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        Grid.SetColumn(_legend, stacked ? 0 : 1);
        Grid.SetRow(_legend, stacked ? 1 : 0);
    }

    public StatisticsDonutChart? Chart
    {
        get => GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    public string? EmptyText
    {
        get => GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChartProperty || change.Property == EmptyTextProperty)
            Rebuild();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Rebuild();
    }

    private void Rebuild()
    {
        var chart = Chart ?? StatisticsDonutChart.Empty;
        _content.IsVisible = chart.HasData;
        _empty.IsVisible = !chart.HasData;
        _empty.Text = EmptyText;
        _legend.Children.Clear();
        if (!chart.HasData)
        {
            _pie.Series = [];
            return;
        }

        var separator = ChartPaints.Paint(this, "SurfaceCardBackgroundBrush", 2);
        _pie.Series = chart.Slices
            .Select(slice => (ISeries)new PieSeries<double>
            {
                Name = slice.Label,
                Values = [slice.Value],
                Fill = ChartPaints.Paint(this, slice.BrushKey),
                Stroke = separator,
                InnerRadius = PieSize * 0.3,
                HoverPushout = 4
            })
            .ToArray();
        _total.Text = chart.Total;

        foreach (var slice in chart.Slices)
            _legend.Children.Add(BuildLegendRow(slice));
    }

    private static Control BuildLegendRow(StatisticsDonutSlice slice)
    {
        var swatch = new Border { Classes = { "stat-legend-swatch" } };
        swatch[!Border.BackgroundProperty] = swatch.GetResourceObservable(slice.BrushKey).ToBinding();

        var label = new TextBlock
        {
            Classes = { "stat-legend-label" },
            Text = slice.Label,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(label, slice.Label);

        var value = new TextBlock
        {
            Classes = { "stat-legend-value" },
            VerticalAlignment = VerticalAlignment.Center,
            Inlines =
            [
                new Run(slice.FormattedValue),
                new Run("  " + slice.PercentText) { Classes = { "stat-legend-percent" } }
            ]
        };

        Grid.SetColumn(label, 1);
        Grid.SetColumn(value, 2);
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 8,
            Children = { swatch, label, value }
        };
    }
}
