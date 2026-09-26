// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Statistics;

/// <summary>
/// Période analysée (bornes incluses) et découpage en intervalles pour les graphiques.
/// </summary>
public sealed record StatisticsPeriod(DateOnly Start, DateOnly End, StatisticsGranularity Granularity)
{
    public DateOnly? PreviousStart { get; private init; }
    public DateOnly? PreviousEnd { get; private init; }

    public static StatisticsPeriod Create(StatisticsRange range, DateOnly today, DateOnly? earliestData)
    {
        var start = range switch
        {
            StatisticsRange.ThisYear => new DateOnly(today.Year, 1, 1),
            StatisticsRange.LastSixMonths => today.AddMonths(-6).AddDays(1),
            StatisticsRange.ThisMonth => new DateOnly(today.Year, today.Month, 1),
            StatisticsRange.ThisWeek => StartOfWeek(today),
            _ => earliestData is { } earliest && earliest < today ? earliest : today
        };

        var granularity = range switch
        {
            StatisticsRange.ThisWeek or StatisticsRange.ThisMonth => StatisticsGranularity.Day,
            StatisticsRange.LastSixMonths => StatisticsGranularity.Week,
            StatisticsRange.ThisYear => StatisticsGranularity.Month,
            _ => GranularityForSpan(start, today)
        };

        var period = new StatisticsPeriod(start, today, granularity);
        if (range == StatisticsRange.All)
            return period;

        var length = today.DayNumber - start.DayNumber + 1;
        return period with
        {
            PreviousStart = start.AddDays(-length),
            PreviousEnd = start.AddDays(-1)
        };
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public bool ContainsPrevious(DateOnly date) =>
        PreviousStart is { } start && PreviousEnd is { } end && date >= start && date <= end;

    public IReadOnlyList<DateOnly> Buckets()
    {
        var buckets = new List<DateOnly>();
        var last = BucketStart(End);
        for (var cursor = BucketStart(Start); cursor <= last; cursor = NextBucket(cursor))
            buckets.Add(cursor);

        return buckets;
    }

    public DateOnly BucketStart(DateOnly date) =>
        Granularity switch
        {
            StatisticsGranularity.Week => StartOfWeek(date),
            StatisticsGranularity.Month => new DateOnly(date.Year, date.Month, 1),
            StatisticsGranularity.Year => new DateOnly(date.Year, 1, 1),
            _ => date
        };

    /// <summary>Additionne les valeurs datées de la période dans chaque intervalle (non cumulé).</summary>
    public IReadOnlyList<StatisticsBucketValue> Timeline<T>(IEnumerable<T> items, Func<T, DateOnly> date, Func<T, double> value)
    {
        var totals = items
            .Where(item => Contains(date(item)))
            .GroupBy(item => BucketStart(date(item)))
            .ToDictionary(group => group.Key, group => group.Sum(value));

        return Buckets()
            .Select(bucket => new StatisticsBucketValue(bucket, totals.GetValueOrDefault(bucket)))
            .ToList();
    }

    private DateOnly NextBucket(DateOnly date) =>
        Granularity switch
        {
            StatisticsGranularity.Week => date.AddDays(7),
            StatisticsGranularity.Month => date.AddMonths(1),
            StatisticsGranularity.Year => date.AddYears(1),
            _ => date.AddDays(1)
        };

    private static StatisticsGranularity GranularityForSpan(DateOnly start, DateOnly end)
    {
        var days = end.DayNumber - start.DayNumber;
        if (days <= 31)
            return StatisticsGranularity.Day;
        if (days <= 200)
            return StatisticsGranularity.Week;

        var months = (end.Year - start.Year) * 12 + end.Month - start.Month;
        return months <= 36 ? StatisticsGranularity.Month : StatisticsGranularity.Year;
    }

    private static DateOnly StartOfWeek(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}
