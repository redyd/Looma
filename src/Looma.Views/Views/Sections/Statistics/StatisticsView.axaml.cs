// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    private double? _yMin;
    private double? _yMax;
    private bool _isPanningY;
    private double _lastPanY;

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

        ResetYAxis();
        RebuildCharts();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StatisticsViewModel.Series)
            or nameof(StatisticsViewModel.Labels)
            or nameof(StatisticsViewModel.HasData))
        {
            ResetYAxis();
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
                MinLimit = _yMin ?? 0,
                MaxLimit = _yMax,
                LabelsPaint = axisPaint,
                SeparatorsPaint = gridPaint,
                TextSize = 12
            }
        ];
    }

    private void OnChartPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel is null || !_viewModel.HasData)
            return;

        var position = e.GetPosition(LineChart);
        var center = LineChart.Bounds.Height <= 0
            ? 0.5
            : Math.Clamp(1 - position.Y / LineChart.Bounds.Height, 0, 1);
        var factor = e.Delta.Y > 0 ? 0.85 : 1.18;

        ZoomYAxis(factor, center);
        e.Handled = true;
    }

    private void OnChartPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(LineChart);
        if (!point.Properties.IsLeftButtonPressed || _viewModel is null || !_viewModel.HasData)
            return;

        _isPanningY = true;
        _lastPanY = point.Position.Y;
        e.Pointer.Capture(LineChart);
        e.Handled = true;
    }

    private void OnChartPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanningY)
            return;

        var point = e.GetCurrentPoint(LineChart);
        if (!point.Properties.IsLeftButtonPressed)
        {
            EndYPan(e.Pointer);
            return;
        }

        PanYAxis(_lastPanY - point.Position.Y);
        _lastPanY = point.Position.Y;
        e.Handled = true;
    }

    private void OnChartPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndYPan(e.Pointer);
        e.Handled = true;
    }

    private void ZoomYAxis(double factor, double centerRatio)
    {
        var (fullMin, fullMax) = GetYBounds();
        var fullSpan = fullMax - fullMin;
        if (fullSpan <= 0)
            return;

        var min = _yMin ?? fullMin;
        var max = _yMax ?? fullMax;
        var span = max - min;
        var newSpan = Math.Clamp(span * factor, fullSpan * 0.05, fullSpan);
        var center = min + span * centerRatio;

        SetYWindow(
            center - newSpan * centerRatio,
            center + newSpan * (1 - centerRatio),
            fullMin,
            fullMax);
    }

    private void PanYAxis(double pixelDelta)
    {
        if (_yMin is null || _yMax is null || LineChart.Bounds.Height <= 0)
            return;

        var (fullMin, fullMax) = GetYBounds();
        var span = _yMax.Value - _yMin.Value;
        if (span <= 0)
            return;

        var valueDelta = pixelDelta / LineChart.Bounds.Height * span;
        SetYWindow(_yMin.Value + valueDelta, _yMax.Value + valueDelta, fullMin, fullMax);
    }

    private void SetYWindow(double min, double max, double fullMin, double fullMax)
    {
        var span = max - min;
        if (span <= 0)
            return;

        if (min < fullMin)
        {
            max += fullMin - min;
            min = fullMin;
        }

        if (max > fullMax)
        {
            min -= max - fullMax;
            max = fullMax;
        }

        _yMin = Math.Max(fullMin, min);
        _yMax = Math.Min(fullMax, max);

        if (Math.Abs(_yMin.Value - fullMin) < 0.0001
            && Math.Abs(_yMax.Value - fullMax) < 0.0001)
        {
            ResetYAxis();
        }

        ApplyYAxisLimits();
    }

    private (double Min, double Max) GetYBounds()
    {
        var max = _viewModel?.Series
            .SelectMany(series => series.Points)
            .Select(point => point.Value)
            .DefaultIfEmpty(1)
            .Max() ?? 1;

        return (0, Math.Max(1, max * 1.1));
    }

    private void ApplyYAxisLimits()
    {
        var yAxis = LineChart.YAxes.FirstOrDefault();
        if (yAxis is null)
            return;

        yAxis.MinLimit = _yMin ?? 0;
        yAxis.MaxLimit = _yMax;
    }

    private void EndYPan(IPointer pointer)
    {
        if (!_isPanningY)
            return;

        _isPanningY = false;
        pointer.Capture(null);
    }

    private void ResetYAxis()
    {
        _yMin = null;
        _yMax = null;
        _isPanningY = false;
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
}
