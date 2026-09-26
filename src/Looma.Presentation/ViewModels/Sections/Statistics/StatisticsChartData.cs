// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Presentation.ViewModels.Sections.Statistics;

/// <summary>Série d'un graphique en colonnes ; <see cref="BrushKey"/> désigne une ressource du thème.</summary>
public sealed record StatisticsChartSeries(string Name, IReadOnlyList<double> Values, string BrushKey);

public sealed record StatisticsTimelineChart(
    IReadOnlyList<string> Labels,
    IReadOnlyList<StatisticsChartSeries> Series,
    Func<double, string> FormatValue,
    bool IsCount = false)
{
    public static StatisticsTimelineChart Empty { get; } = new([], [], value => value.ToString("N0"));

    public bool HasData => Series.Any(series => series.Values.Any(value => value > 0));
}

public sealed record StatisticsDonutSlice(string Label, double Value, string FormattedValue, double Percent, string BrushKey)
{
    public string PercentText => Percent.ToString("P0");
}

public sealed record StatisticsDonutChart(IReadOnlyList<StatisticsDonutSlice> Slices, string Total)
{
    public static StatisticsDonutChart Empty { get; } = new([], string.Empty);

    public bool HasData => Slices.Count > 0;
}
