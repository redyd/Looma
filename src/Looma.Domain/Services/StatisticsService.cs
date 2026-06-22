// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.IServices;
using Looma.Domain.Repositories;
using Looma.Domain.Statistics;

namespace Looma.Domain.Services;

public sealed class StatisticsService(
    ITrackedWoolRepository trackedWoolRepository,
    IWoolRepository woolRepository)
    : IStatisticsService
{
    public async Task<ResultT<StatisticsSnapshot>> GetAsync(StatisticsQuery query)
    {
        var movementsResult = await trackedWoolRepository.GetMovementsAsync();
        if (movementsResult.Failed || movementsResult.Value is null)
            return ResultT<StatisticsSnapshot>.Failure(movementsResult.Error ?? "Impossible de charger les mouvements de laine.");

        var woolsResult = await woolRepository.GetAllAsync();
        if (woolsResult.Failed || woolsResult.Value is null)
            return ResultT<StatisticsSnapshot>.Failure(woolsResult.Error ?? "Impossible de charger les laines.");

        return ResultT<StatisticsSnapshot>.Ok(BuildUsedWoolByWoolLine(movementsResult.Value, woolsResult.Value, query));
    }

    private static StatisticsSnapshot BuildUsedWoolByWoolLine(
        IReadOnlyList<TrackedWoolMovement> movements,
        IReadOnlyList<Wool> wools,
        StatisticsQuery query)
    {
        var usedMovements = movements
            .Where(m => m.Quantity < 0)
            .Where(m => query.PatternType is null || m.PatternType == query.PatternType)
            .ToList();
        if (usedMovements.Count == 0)
            return new StatisticsSnapshot([], [], []);

        var minDate = GetMinDate(query);
        var firstAvailableDate = DateOnly.FromDateTime(usedMovements.Min(m => m.Date));
        var firstDate = minDate ?? firstAvailableDate;
        if (query.Range == StatisticsRange.All && firstAvailableDate < firstDate)
            firstDate = firstAvailableDate;

        var buckets = BuildBuckets(firstDate, query.Today, query.Range);
        var woolsById = wools.ToDictionary(w => w.Id);
        var series = usedMovements
            .GroupBy(m => new
            {
                m.WoolId,
                m.WoolBrand,
                m.WoolName
            })
            .Select(group => new StatisticsSeries(
                $"{group.Key.WoolBrand} - {group.Key.WoolName}",
                BuildUsedWoolPoints(group, buckets, query.Range, query.QuantityUnit, woolsById.GetValueOrDefault(group.Key.WoolId))))
            .Where(s => s.Points.Any(p => p.Value > 0))
            .OrderBy(s => s.Name)
            .ToList();

        return new StatisticsSnapshot(
            buckets.Select(b => b.Label).ToList(),
            series,
            []);
    }

    private static IReadOnlyList<StatisticsPoint> BuildUsedWoolPoints(
        IEnumerable<TrackedWoolMovement> movements,
        IReadOnlyList<StatisticsBucket> buckets,
        StatisticsRange range,
        StatisticsQuantityUnit quantityUnit,
        Wool? wool)
    {
        var orderedMovements = movements
            .OrderBy(m => m.Date)
            .ToList();

        return buckets
            .Select(bucket =>
            {
                var nextBucketStart = AddBucket(bucket.Start, range).ToDateTime(TimeOnly.MinValue);
                var value = orderedMovements
                    .Where(m => m.Date < nextBucketStart)
                    .Sum(m => ConvertQuantity(Math.Abs(m.Quantity), quantityUnit, wool));

                return new StatisticsPoint(bucket.Label, bucket.Start, value);
            })
            .ToList();
    }

    private static IReadOnlyList<StatisticsBucket> BuildBuckets(DateOnly firstDate, DateOnly today, StatisticsRange range)
    {
        var first = BucketStart(firstDate, range);
        var last = BucketStart(today, range);
        if (first > last)
            last = first;

        var buckets = new List<StatisticsBucket>();
        for (var cursor = first; cursor <= last; cursor = AddBucket(cursor, range))
        {
            buckets.Add(new StatisticsBucket(cursor, FormatBucketLabel(cursor, range)));
        }

        return buckets;
    }

    private static DateOnly? GetMinDate(StatisticsQuery query) =>
        query.Range switch
        {
            StatisticsRange.All => null,
            StatisticsRange.ThisYear => new DateOnly(query.Today.Year, 1, 1),
            StatisticsRange.LastSixMonths => query.Today.AddMonths(-6),
            StatisticsRange.ThisMonth => new DateOnly(query.Today.Year, query.Today.Month, 1),
            StatisticsRange.ThisWeek => query.Today.AddDays(-DaysSinceMonday(query.Today)),
            _ => null
        };

    private static DateOnly BucketStart(DateOnly date, StatisticsRange range) =>
        range is StatisticsRange.ThisMonth or StatisticsRange.LastSixMonths or StatisticsRange.ThisWeek
            ? date
            : new DateOnly(date.Year, date.Month, 1);

    private static DateOnly AddBucket(DateOnly date, StatisticsRange range) =>
        range is StatisticsRange.ThisMonth or StatisticsRange.LastSixMonths or StatisticsRange.ThisWeek
            ? date.AddDays(1)
            : date.AddMonths(1);

    private static string FormatBucketLabel(DateOnly date, StatisticsRange range) =>
        range is StatisticsRange.ThisMonth or StatisticsRange.LastSixMonths or StatisticsRange.ThisWeek
            ? date.ToString("dd/MM")
            : date.ToString("MM/yyyy");

    private static double ConvertQuantity(double quantity, StatisticsQuantityUnit quantityUnit, Wool? wool)
    {
        var skeins = quantity / 1000;
        return quantityUnit switch
        {
            StatisticsQuantityUnit.Weight => skeins * (wool?.Weight ?? 0),
            StatisticsQuantityUnit.Length => skeins * (wool?.Length ?? 0),
            _ => skeins
        };
    }

    private static int DaysSinceMonday(DateOnly date) =>
        date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - (int)DayOfWeek.Monday;

    private sealed record StatisticsBucket(DateOnly Start, string Label);
}
