// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;
using Looma.Domain.IServices;
using Looma.Domain.Repositories;
using Looma.Domain.Statistics;

namespace Looma.Domain.Services;

public sealed class StatisticsService(
    ITrackedWoolRepository trackedWoolRepository,
    IProjectRepository projectRepository)
    : IStatisticsService
{
    public async Task<ResultT<StatisticsSnapshot>> GetAsync(StatisticsQuery query)
    {
        var minDate = GetMinDate(query);

        if (query.DataKind == StatisticsDataKind.Project && query.ChartKind == StatisticsChartKind.Pie)
        {
            var projectsResult = await projectRepository.GetAllAsync();
            if (projectsResult.Failed || projectsResult.Value is null)
                return ResultT<StatisticsSnapshot>.Failure(projectsResult.Error ?? "Impossible de charger les projets.");

            return ResultT<StatisticsSnapshot>.Ok(BuildProjectPie(projectsResult.Value, query, minDate));
        }

        var movementFrom = query.ChartKind == StatisticsChartKind.Line
            ? null
            : minDate?.ToDateTime(TimeOnly.MinValue);
        var movementsResult = await trackedWoolRepository.GetMovementsAsync(movementFrom);
        if (movementsResult.Failed || movementsResult.Value is null)
            return ResultT<StatisticsSnapshot>.Failure(movementsResult.Error ?? "Impossible de charger les mouvements de laine.");

        var movements = movementsResult.Value;
        var snapshot = query.DataKind switch
        {
            StatisticsDataKind.Wool when query.ChartKind == StatisticsChartKind.Line => BuildWoolLine(movements, query, minDate),
            StatisticsDataKind.Wool => BuildWoolPie(movements),
            StatisticsDataKind.Project => BuildProjectLine(movements, query, minDate),
            _ => new StatisticsSnapshot([], [], [])
        };

        return ResultT<StatisticsSnapshot>.Ok(snapshot);
    }

    private static StatisticsSnapshot BuildWoolLine(
        IReadOnlyList<TrackedWoolMovement> movements,
        StatisticsQuery query,
        DateOnly? minDate)
    {
        var rangeDates = movements
            .Select(m => DateOnly.FromDateTime(m.Date))
            .Where(date => minDate is null || date >= minDate)
            .ToList();

        if (minDate is not null && movements.Any(m => DateOnly.FromDateTime(m.Date) < minDate))
            rangeDates.Add(minDate.Value);

        var buckets = BuildBuckets(rangeDates, query, minDate);
        var series = movements
            .GroupBy(m => new
            {
                m.WoolId,
                m.WoolBrand,
                m.WoolName
            })
            .Select(group => new StatisticsSeries(
                $"{group.Key.WoolBrand} - {group.Key.WoolName}",
                BuildWoolStockPoints(group, buckets)))
            .Where(series => series.Points.Any(point => point.Value > 0))
            .OrderBy(series => series.Name)
            .Take(8)
            .ToList();

        return new StatisticsSnapshot(
            buckets.Select(b => b.Label).ToList(),
            series,
            []);
    }

    private static IReadOnlyList<StatisticsPoint> BuildWoolStockPoints(
        IEnumerable<TrackedWoolMovement> movements,
        IReadOnlyList<StatisticsBucket> buckets)
    {
        var orderedMovements = movements
            .OrderBy(m => m.Date)
            .ToList();

        return buckets
            .Select(bucket =>
            {
                var value = orderedMovements
                    .Where(m => BucketStart(DateOnly.FromDateTime(m.Date), bucket.Range) <= bucket.Start)
                    .Sum(ToSkeinQuantity);

                return new StatisticsPoint(bucket.Label, bucket.Start, Math.Max(0, value));
            })
            .ToList();
    }

    private static StatisticsSnapshot BuildProjectLine(
        IReadOnlyList<TrackedWoolMovement> movements,
        StatisticsQuery query,
        DateOnly? minDate)
    {
        var projectMovements = movements
            .Where(m => m.Quantity < 0 && m.ProjectId is not null)
            .Where(m => query.PatternType is null || m.PatternType == query.PatternType)
            .Where(m => query.ProjectStatus is null || m.ProjectStatus == query.ProjectStatus)
            .ToList();

        var rangeDates = projectMovements
            .Select(m => DateOnly.FromDateTime(m.Date))
            .Where(date => minDate is null || date >= minDate)
            .ToList();

        if (minDate is not null && projectMovements.Any(m => DateOnly.FromDateTime(m.Date) < minDate))
            rangeDates.Add(minDate.Value);

        var buckets = BuildBuckets(rangeDates, query, minDate);
        var series = projectMovements
            .GroupBy(m => new
            {
                ProjectId = m.ProjectId!.Value,
                ProjectName = m.ProjectName ?? $"Projet {m.ProjectId}"
            })
            .Select(group => new StatisticsSeries(
                group.Key.ProjectName,
                BuildProjectUsagePoints(group, buckets)))
            .Where(series => series.Points.Any(point => point.Value > 0))
            .OrderBy(series => series.Name)
            .Take(8)
            .ToList();

        return new StatisticsSnapshot(
            buckets.Select(b => b.Label).ToList(),
            series,
            []);
    }

    private static IReadOnlyList<StatisticsPoint> BuildProjectUsagePoints(
        IEnumerable<TrackedWoolMovement> movements,
        IReadOnlyList<StatisticsBucket> buckets)
    {
        var orderedMovements = movements
            .OrderBy(m => m.Date)
            .ToList();

        return buckets
            .Select(bucket =>
            {
                var value = orderedMovements
                    .Where(m => BucketStart(DateOnly.FromDateTime(m.Date), bucket.Range) <= bucket.Start)
                    .Sum(m => Math.Abs(ToSkeinQuantity(m)));

                return new StatisticsPoint(bucket.Label, bucket.Start, value);
            })
            .ToList();
    }

    private static StatisticsSnapshot BuildWoolPie(IReadOnlyList<TrackedWoolMovement> movements)
    {
        var slices = movements
            .Where(m => m.Quantity < 0)
            .GroupBy(m => $"{m.WoolBrand} - {m.WoolName}")
            .Select(g => new StatisticsSlice(g.Key, g.Sum(m => Math.Abs(ToSkeinQuantity(m)))))
            .Where(s => s.Value > 0)
            .OrderByDescending(s => s.Value)
            .ThenBy(s => s.Label)
            .Take(8)
            .ToList();

        return new StatisticsSnapshot([], [], slices);
    }

    private static StatisticsSnapshot BuildProjectPie(
        IReadOnlyList<Project> projects,
        StatisticsQuery query,
        DateOnly? minDate)
    {
        var filtered = projects
            .Where(p => p.BeginDate is null || minDate is null || p.BeginDate >= minDate)
            .Where(p => query.PatternType is null || p.Pattern?.Type == query.PatternType)
            .Where(p => query.ProjectStatus is null || p.Status == query.ProjectStatus)
            .ToList();

        var slices = query.ProjectGrouping == StatisticsProjectGrouping.Status
            ? filtered
                .GroupBy(p => p.Status.GetDisplayName())
                .Select(g => new StatisticsSlice(g.Key, g.Count()))
            : filtered
                .GroupBy(p => p.Pattern?.Type.GetDisplayName() ?? "Sans patron")
                .Select(g => new StatisticsSlice(g.Key, g.Count()));

        return new StatisticsSnapshot(
            [],
            [],
            slices
                .Where(s => s.Value > 0)
                .OrderByDescending(s => s.Value)
                .ThenBy(s => s.Label)
                .ToList());
    }

    private static IReadOnlyList<StatisticsPoint> SumMovements(
        IReadOnlyList<StatisticsBucket> buckets,
        IEnumerable<TrackedWoolMovement> movements,
        Func<TrackedWoolMovement, double> selector)
    {
        var grouped = movements
            .GroupBy(m => BucketStart(DateOnly.FromDateTime(m.Date), buckets.FirstOrDefault()?.Range ?? StatisticsRange.ThisMonth))
            .ToDictionary(g => g.Key, g => g.Sum(selector));

        return buckets
            .Select(bucket => new StatisticsPoint(
                bucket.Label,
                bucket.Start,
                grouped.GetValueOrDefault(bucket.Start)))
            .ToList();
    }

    private static IReadOnlyList<StatisticsBucket> BuildBuckets(
        IEnumerable<DateOnly> dates,
        StatisticsQuery query,
        DateOnly? minDate)
    {
        var dateList = dates
            .Where(date => minDate is null || date >= minDate)
            .OrderBy(date => date)
            .ToList();

        if (dateList.Count == 0)
            return [];

        var first = BucketStart(minDate ?? dateList.First(), query.Range);
        var last = BucketStart(dateList.Last() > query.Today ? dateList.Last() : query.Today, query.Range);
        var buckets = new List<StatisticsBucket>();

        for (var cursor = first; cursor <= last; cursor = AddBucket(cursor, query.Range))
        {
            buckets.Add(new StatisticsBucket(cursor, FormatBucketLabel(cursor, query.Range), query.Range));
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
            _ => null
        };

    private static DateOnly BucketStart(DateOnly date, StatisticsRange range) =>
        range is StatisticsRange.ThisMonth or StatisticsRange.LastSixMonths
            ? date
            : new DateOnly(date.Year, date.Month, 1);

    private static DateOnly AddBucket(DateOnly date, StatisticsRange range) =>
        range is StatisticsRange.ThisMonth or StatisticsRange.LastSixMonths
            ? date.AddDays(1)
            : date.AddMonths(1);

    private static double ToSkeinQuantity(TrackedWoolMovement movement) =>
        movement.Quantity / 1000;

    private static string FormatBucketLabel(DateOnly date, StatisticsRange range) =>
        range is StatisticsRange.ThisMonth or StatisticsRange.LastSixMonths
            ? date.ToString("dd/MM")
            : date.ToString("MM/yyyy");

    private sealed record StatisticsBucket(DateOnly Start, string Label, StatisticsRange Range);
}
