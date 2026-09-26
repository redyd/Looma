// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using Looma.Presentation.ViewModels.Sections.Statistics;

namespace Looma.Views.UserControls.Statistics;

/// <summary>Graphique en colonnes groupées pour une <see cref="StatisticsTimelineChart"/>.</summary>
public sealed class TimelineChart : UserControl
{
    public static readonly StyledProperty<StatisticsTimelineChart?> ChartProperty =
        AvaloniaProperty.Register<TimelineChart, StatisticsTimelineChart?>(nameof(Chart));

    public static readonly StyledProperty<string?> EmptyTextProperty =
        AvaloniaProperty.Register<TimelineChart, string?>(nameof(EmptyText));

    private readonly CartesianChart _chart = new()
    {
        LegendPosition = LegendPosition.Hidden,
        TooltipPosition = TooltipPosition.Top,
        FindingStrategy = FindingStrategy.ExactMatch
    };

    private readonly TextBlock _empty = new()
    {
        Classes = { "empty-state-message" },
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        TextAlignment = Avalonia.Media.TextAlignment.Center
    };

    private readonly StackPanel _legend = new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 16,
        Margin = new Thickness(0, 0, 0, 8)
    };

    public TimelineChart()
    {
        var chartHost = new Grid { Children = { _chart, _empty } };
        Grid.SetRow(chartHost, 1);
        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Children = { _legend, chartHost }
        };
        ActualThemeVariantChanged += (_, _) => Rebuild();
    }

    public StatisticsTimelineChart? Chart
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
        var chart = Chart ?? StatisticsTimelineChart.Empty;
        var hasData = chart.HasData;
        _chart.IsVisible = hasData;
        _empty.IsVisible = !hasData;
        _empty.Text = EmptyText;
        _legend.IsVisible = hasData;
        _legend.Children.Clear();
        if (!hasData)
        {
            _chart.Series = [];
            return;
        }

        foreach (var series in chart.Series)
            _legend.Children.Add(BuildLegendItem(series));

        var axisPaint = ChartPaints.Paint(this, "TextSecondaryBrush");
        var gridPaint = ChartPaints.Paint(this, "DividerBrush");
        var surfacePaint = ChartPaints.Paint(this, "SurfaceCardBackgroundBrush");

        _chart.TooltipTextPaint = ChartPaints.Paint(this, "TextPrimaryBrush");
        _chart.TooltipBackgroundPaint = surfacePaint;
        _chart.Series = chart.Series
            .Select(series => (ISeries)new ColumnSeries<double>
            {
                Name = series.Name,
                Values = series.Values.ToArray(),
                Fill = ChartPaints.Paint(this, series.BrushKey),
                Stroke = null,
                MaxBarWidth = 22,
                Padding = 2,
                Rx = 4,
                Ry = 4,
                YToolTipLabelFormatter = point => chart.FormatValue(point.Coordinate.PrimaryValue)
            })
            .ToArray();

        _chart.XAxes =
        [
            new Axis
            {
                Labels = chart.Labels.ToArray(),
                LabelsPaint = axisPaint,
                TextSize = 12,
                MinStep = 1,
                LabelsRotation = chart.Labels.Count > 16 ? -45 : 0
            }
        ];
        _chart.YAxes =
        [
            new Axis
            {
                MinLimit = 0,
                MinStep = chart.IsCount ? 1 : 0,
                LabelsPaint = axisPaint,
                SeparatorsPaint = gridPaint,
                TextSize = 12,
                Labeler = value => chart.FormatValue(value)
            }
        ];
    }

    private static Control BuildLegendItem(StatisticsChartSeries series)
    {
        var swatch = new Border { Classes = { "stat-legend-swatch" } };
        swatch[!Border.BackgroundProperty] = swatch.GetResourceObservable(series.BrushKey).ToBinding();
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                swatch,
                new TextBlock { Classes = { "stat-legend-label" }, Text = series.Name, VerticalAlignment = VerticalAlignment.Center }
            }
        };
    }
}
