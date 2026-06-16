// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Looma.Views.UserControls;

public partial class ColorPieControl : Control
{
    public static readonly StyledProperty<IList<string>> ColorsProperty =
        AvaloniaProperty.Register<ColorPieControl, IList<string>>(nameof(Colors), []);

    public static readonly StyledProperty<IBrush> BorderBrushProperty =
        AvaloniaProperty.Register<ColorPieControl, IBrush>(nameof(BorderBrush), Brushes.Transparent);

    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<ColorPieControl, double>(nameof(BorderThickness), 1.5);

    public IList<string> Colors
    {
        get => GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    public IBrush BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public double BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    static ColorPieControl()
    {
        AffectsRender<ColorPieControl>(ColorsProperty, BorderBrushProperty, BorderThicknessProperty);
        AffectsMeasure<ColorPieControl>(ColorsProperty);
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        var radius = size / 2.0;
        var center = new Point(Bounds.Width / 2.0, Bounds.Height / 2.0);
        var pen = new Pen(BorderBrush, BorderThickness);

        var validColors = Colors?
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList() ?? [];

        if (validColors.Count == 0)
        {
            DrawHatched(context, center, radius, pen);
            return;
        }

        if (Colors == null || Colors.Count == 0)
        {
            DrawHatched(context, center, radius, pen);
            return;
        }

        if (Colors.Count == 1)
        {
            var brush = ParseBrush(Colors[0]);
            context.DrawEllipse(brush, pen, center, radius, radius);
            return;
        }

        double anglePerSlice = 360.0 / Colors.Count;
        double startAngle = -90.0;

        for (int i = 0; i < Colors.Count; i++)
        {
            var brush = ParseBrush(Colors[i]);
            DrawSlice(context, center, radius, startAngle, anglePerSlice, brush);
            startAngle += anglePerSlice;
        }
    }

    private static void DrawSlice(DrawingContext context, Point center, double radius,
        double startAngleDeg, double sweepAngleDeg, IBrush brush)
    {
        var startRad = ToRad(startAngleDeg);
        var endRad = ToRad(startAngleDeg + sweepAngleDeg);

        var startPoint = new Point(
            center.X + radius * Math.Cos(startRad),
            center.Y + radius * Math.Sin(startRad));

        var endPoint = new Point(
            center.X + radius * Math.Cos(endRad),
            center.Y + radius * Math.Sin(endRad));

        var isLargeArc = sweepAngleDeg > 180.0;

        var geometry = new StreamGeometry();
        using (var gc = geometry.Open())
        {
            gc.BeginFigure(center);
            gc.LineTo(startPoint);
            gc.ArcTo(endPoint, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise);
            gc.EndFigure(true);
        }

        context.DrawGeometry(brush, new Pen(Brushes.Transparent, 0), geometry);
    }

    private static void DrawHatched(DrawingContext context, Point center, double radius, Pen pen)
    {
        var hatchBrush = new DrawingBrush
        {
            TileMode = TileMode.Tile,
            SourceRect = new RelativeRect(0, 0, 8, 8, RelativeUnit.Absolute),
            DestinationRect = new RelativeRect(0, 0, 8, 8, RelativeUnit.Absolute),
            Drawing = new GeometryDrawing
            {
                Pen = new Pen(Brushes.LightGray),
                Geometry = Geometry.Parse("M 0,8 L 8,0")
            }
        };

        context.DrawEllipse(hatchBrush, pen, center, radius, radius);
    }

    private static IBrush ParseBrush(string hex)
    {
        try { return new SolidColorBrush(Color.Parse(hex)); }
        catch { return Brushes.Transparent; }
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}