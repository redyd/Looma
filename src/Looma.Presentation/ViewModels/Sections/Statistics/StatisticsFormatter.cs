// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Globalization;
using Looma.Domain.Statistics;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Sections.Statistics;

/// <summary>Mise en forme des quantités, dates et parts pour la page Statistiques (culture courante).</summary>
public static class StatisticsFormatter
{
    private const int MaxDonutSlices = 6;

    public static string Quantity(double value, StatisticsQuantityUnit unit, TranslationService translation)
    {
        var culture = CultureInfo.CurrentCulture;
        return unit switch
        {
            StatisticsQuantityUnit.Weight when value >= 1000 => string.Format(culture, "{0:#,0.##} kg", value / 1000),
            StatisticsQuantityUnit.Weight => string.Format(culture, "{0:#,0} g", value),
            StatisticsQuantityUnit.Length when value >= 1000 => string.Format(culture, "{0:#,0.##} km", value / 1000),
            StatisticsQuantityUnit.Length => string.Format(culture, "{0:#,0} m", value),
            _ => translation.Format("Statistics_Format_Skeins", value.ToString("#,0.#", culture))
        };
    }

    public static string Count(double value) => value.ToString("#,0", CultureInfo.CurrentCulture);

    public static string Days(double days, TranslationService translation) =>
        translation.Format("Statistics_Format_Days", Math.Round(days).ToString("#,0", CultureInfo.CurrentCulture));

    public static string BucketLabel(DateOnly start, StatisticsGranularity granularity)
    {
        var culture = CultureInfo.CurrentCulture;
        return granularity switch
        {
            StatisticsGranularity.Year => start.ToString("yyyy", culture),
            StatisticsGranularity.Month => start.ToString("MMM yy", culture),
            _ => start.ToString("d MMM", culture)
        };
    }

    public static (StatisticsTrend Trend, string? Text) Trend(double current, double? previous, TranslationService translation)
    {
        if (previous is null)
            return (StatisticsTrend.None, null);

        if (previous.Value <= 0)
        {
            return current > 0
                ? (StatisticsTrend.Up, translation["Statistics_Trend_New"])
                : (StatisticsTrend.None, null);
        }

        var percent = (current - previous.Value) / previous.Value;
        if (Math.Abs(percent) < 0.005)
            return (StatisticsTrend.Stable, translation["Statistics_Trend_Stable"]);

        var text = translation.Format(
            "Statistics_Trend_VsPrevious",
            percent.ToString("+0%;-0%", CultureInfo.CurrentCulture));
        return (percent > 0 ? StatisticsTrend.Up : StatisticsTrend.Down, text);
    }

    public static StatisticsTimelineChart Timeline(
        StatisticsGranularity granularity,
        IReadOnlyList<(string Name, IReadOnlyList<StatisticsBucketValue> Values, string BrushKey)> series,
        Func<double, string> formatValue,
        bool isCount = false)
    {
        var buckets = series.Count == 0 ? [] : series[0].Values;
        return new StatisticsTimelineChart(
            buckets.Select(bucket => BucketLabel(bucket.Start, granularity)).ToList(),
            series
                .Select(s => new StatisticsChartSeries(s.Name, s.Values.Select(v => v.Value).ToList(), s.BrushKey))
                .ToList(),
            formatValue,
            isCount);
    }

    /// <summary>Construit un donut ; au-delà de six parts, les plus petites sont regroupées dans « Autres ».</summary>
    public static StatisticsDonutChart Donut<TKey>(
        IReadOnlyList<StatisticsShare<TKey>> shares,
        Func<TKey, string> label,
        Func<TKey, int, string> brushKey,
        Func<double, string> formatValue,
        TranslationService translation)
    {
        var total = shares.Sum(share => share.Value);
        if (total <= 0)
            return StatisticsDonutChart.Empty;

        var slices = shares
            .Take(shares.Count > MaxDonutSlices ? MaxDonutSlices - 1 : MaxDonutSlices)
            .Select((share, index) => new StatisticsDonutSlice(
                label(share.Key),
                share.Value,
                formatValue(share.Value),
                share.Value / total,
                brushKey(share.Key, index)))
            .ToList();

        if (shares.Count > MaxDonutSlices)
        {
            var others = shares.Skip(MaxDonutSlices - 1).Sum(share => share.Value);
            slices.Add(new StatisticsDonutSlice(
                translation["Statistics_Other"],
                others,
                formatValue(others),
                others / total,
                OtherBrush));
        }

        return new StatisticsDonutChart(slices, formatValue(total));
    }

    /// <summary>Couleur neutre de la part « Autres », distincte des couleurs de séries.</summary>
    public const string OtherBrush = "TextSecondaryBrush";

    public static string SeriesBrush(int index) => $"ChartSeries{index % 8 + 1}Brush";
}
