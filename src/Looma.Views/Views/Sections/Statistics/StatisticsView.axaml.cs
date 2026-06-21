// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Looma.Presentation.ViewModels.Sections.Statistics;
using SkiaSharp;

namespace Looma.Views.Views.Sections.Statistics;

public partial class StatisticsView : UserControl
{
    private StatisticsViewModel? _viewModel;

    public StatisticsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as StatisticsViewModel;

        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        RebuildCharts();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StatisticsViewModel.Series)
            or nameof(StatisticsViewModel.Slices)
            or nameof(StatisticsViewModel.Labels)
            or nameof(StatisticsViewModel.HasData))
        {
            RebuildCharts();
        }
    }

    private void RebuildCharts()
    {
        if (_viewModel is null)
            return;

        var axisPaint = BuildPaint("TextSecondaryBrush");
        var primaryTextPaint = BuildPaint("TextPrimaryBrush");
        var gridPaint = BuildPaint("DividerBrush", 1);
        var surfacePaint = BuildPaint("SurfaceCardBackgroundBrush");

        LineChart.LegendTextPaint = axisPaint;
        LineChart.LegendBackgroundPaint = surfacePaint;
        LineChart.TooltipTextPaint = primaryTextPaint;
        LineChart.TooltipBackgroundPaint = surfacePaint;
        LineChart.FindingStrategy = FindingStrategy.CompareAllTakeClosest;

        LineChart.Series = _viewModel.Series
            .Select((series, index) => new LineSeries<double>
            {
                Name = series.Name,
                Values = series.Points.Select(point => point.Value).ToArray(),
                Fill = null,
                Stroke = BuildPaint($"ChartSeries{index % 8 + 1}Brush", 3),
                GeometryFill = BuildPaint($"ChartSeries{index % 8 + 1}Brush"),
                GeometryStroke = BuildPaint("SurfaceCardBackgroundBrush", 2),
                GeometrySize = 8
            })
            .Cast<ISeries>()
            .ToArray();
        LineLegend.ItemsSource = _viewModel.Series
            .Select((series, index) => new LineLegendItem(
                series.Name,
                GetBrush($"ChartSeries{index % 8 + 1}Brush")))
            .ToArray();

        LineChart.XAxes =
        [
            new Axis
            {
                Labels = _viewModel.Labels.ToArray(),
                LabelsPaint = axisPaint,
                SeparatorsPaint = gridPaint,
                TextSize = 12
            }
        ];

        LineChart.YAxes =
        [
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = axisPaint,
                SeparatorsPaint = gridPaint,
                TextSize = 12
            }
        ];

        PieChart.Series = _viewModel.Slices
            .Select((slice, index) => new PieSeries<double>
            {
                Name = slice.Label,
                Values = [slice.Value],
                Fill = BuildPaint($"ChartSeries{index % 8 + 1}Brush"),
                Stroke = BuildPaint("SurfaceCardBackgroundBrush", 2),
                DataLabelsPaint = axisPaint,
                DataLabelsSize = 12
            })
            .Cast<ISeries>()
            .ToArray();

        PieChart.LegendTextPaint = axisPaint;
        PieChart.LegendBackgroundPaint = surfacePaint;
        PieChart.TooltipTextPaint = primaryTextPaint;
        PieChart.TooltipBackgroundPaint = surfacePaint;
    }

    private SolidColorPaint BuildPaint(string resourceKey, float strokeThickness = 1)
    {
        var color = Application.Current is not null
            && Application.Current.TryGetResource(resourceKey, null, out var resource)
            && resource is ISolidColorBrush brush
            ? brush.Color
            : Colors.Gray;

        return new SolidColorPaint(new SKColor(color.R, color.G, color.B, color.A))
        {
            StrokeThickness = strokeThickness
        };
    }

    private IBrush GetBrush(string resourceKey) =>
        Application.Current is not null
        && Application.Current.TryGetResource(resourceKey, null, out var resource)
        && resource is IBrush brush
            ? brush
            : Brushes.Gray;

}

public sealed record LineLegendItem(string Label, IBrush Brush);
