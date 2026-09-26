// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.IServices;
using Looma.Domain.Localization;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;
using Looma.Domain.Statistics;

namespace Looma.Domain.Services;

public sealed class StatisticsService(
    ITrackedWoolRepository trackedWoolRepository,
    IWoolRepository woolRepository,
    IProjectRepository projectRepository,
    IPatternRepository patternRepository,
    IDocumentRepository documentRepository,
    IDomainLogger? logger = null)
    : DomainServiceBase(logger), IStatisticsService
{
    private const int RankingSize = 5;

    public Task<ResultT<WoolStatistics>> GetWoolStatisticsAsync(StatisticsFilter filter) =>
        ExecuteAsync("Statistics.GetWool", async () =>
        {
            var movements = await trackedWoolRepository.GetMovementsAsync();
            if (movements.Failed || movements.Value is null)
                return ResultT<WoolStatistics>.Failure(movements.Error ?? Localizer.Get("Statistics_Errors_UnableToLoad"));

            var wools = await woolRepository.GetAllAsync();
            if (wools.Failed || wools.Value is null)
                return ResultT<WoolStatistics>.Failure(wools.Error ?? Localizer.Get("Statistics_Errors_UnableToLoad"));

            return ResultT<WoolStatistics>.Ok(BuildWoolStatistics(movements.Value, wools.Value, filter));
        });

    public Task<ResultT<GlobalStatistics>> GetGlobalStatisticsAsync(StatisticsFilter filter) =>
        ExecuteAsync("Statistics.GetGlobal", async () =>
        {
            var projects = await projectRepository.GetAllAsync();
            if (projects.Failed || projects.Value is null)
                return ResultT<GlobalStatistics>.Failure(projects.Error ?? Localizer.Get("Statistics_Errors_UnableToLoad"));

            var patterns = await patternRepository.GetAllAsync();
            if (patterns.Failed || patterns.Value is null)
                return ResultT<GlobalStatistics>.Failure(patterns.Error ?? Localizer.Get("Statistics_Errors_UnableToLoad"));

            var wools = await woolRepository.GetAllAsync();
            if (wools.Failed || wools.Value is null)
                return ResultT<GlobalStatistics>.Failure(wools.Error ?? Localizer.Get("Statistics_Errors_UnableToLoad"));

            var documents = await documentRepository.GetAllAsync();
            if (documents.Failed || documents.Value is null)
                return ResultT<GlobalStatistics>.Failure(documents.Error ?? Localizer.Get("Statistics_Errors_UnableToLoad"));

            return ResultT<GlobalStatistics>.Ok(BuildGlobalStatistics(
                projects.Value,
                patterns.Value,
                wools.Value.Count,
                documents.Value.Count,
                filter));
        });

    private static WoolStatistics BuildWoolStatistics(
        IReadOnlyList<TrackedWoolMovement> movements,
        IReadOnlyList<Wool> wools,
        StatisticsFilter filter)
    {
        // Le filtre par type de patron ne concerne que la consommation : les achats ne sont liés à aucun projet.
        var used = movements
            .Where(m => m.Quantity < 0)
            .Where(m => filter.PatternType is null || m.PatternType == filter.PatternType)
            .ToList();
        var added = movements.Where(m => m.Quantity > 0).ToList();

        var earliest = used.Concat(added).Select(DateOf).DefaultIfEmpty(filter.Today).Min();
        var period = StatisticsPeriod.Create(filter.Range, filter.Today, earliest);

        var usedInPeriod = used.Where(m => period.Contains(DateOf(m))).ToList();
        double Amount(TrackedWoolMovement m) => Convert(Math.Abs(m.Skeins), filter.Unit, m.WoolWeight, m.WoolLength);

        var stocked = wools.Where(w => w.Stock > 0).ToList();

        return new WoolStatistics
        {
            Granularity = period.Granularity,
            Unit = filter.Unit,
            Used = usedInPeriod.Sum(Amount),
            Added = added.Where(m => period.Contains(DateOf(m))).Sum(Amount),
            UsedPreviousPeriod = period.PreviousStart is null
                ? null
                : used.Where(m => period.ContainsPrevious(DateOf(m))).Sum(Amount),
            CurrentStock = stocked.Sum(w => Convert(w.BatchQuantity, filter.Unit, w.Weight, w.Length)),
            WoolsInStock = stocked.Count,
            ProjectsSupplied = usedInPeriod
                .Where(m => m.ProjectId is not null || m.ProjectName is not null)
                .Select(m => (m.ProjectId, m.ProjectName))
                .Distinct()
                .Count(),
            UsedTimeline = period.Timeline(used, DateOf, Amount),
            AddedTimeline = period.Timeline(added, DateOf, Amount),
            UsedByMaterial = Shares(usedInPeriod, m => NormalizeMaterial(m.WoolMaterial), Amount),
            UsedByWeightClass = Shares(
                usedInPeriod.Where(m => WeightClass(m.WoolNeedleMinSize, m.WoolNeedleMaxSize) is not null),
                m => WeightClass(m.WoolNeedleMinSize, m.WoolNeedleMaxSize)!.Value,
                Amount),
            StockByWeightClass = Shares(
                stocked.Where(w => WeightClass(w.NeedleMinSize, w.NeedleMaxSize) is not null),
                w => WeightClass(w.NeedleMinSize, w.NeedleMaxSize)!.Value,
                w => Convert(w.BatchQuantity, filter.Unit, w.Weight, w.Length)),
            TopWools = usedInPeriod
                .GroupBy(m => (m.WoolId, m.WoolId is null ? m.WoolBrand + "\u001f" + m.WoolName : null))
                .Select(group =>
                {
                    var latest = group.MaxBy(m => m.Date)!;
                    return new StatisticsWoolRanking(
                        group.Key.WoolId,
                        latest.WoolName,
                        latest.WoolBrand,
                        latest.WoolColors,
                        group.Sum(Amount),
                        latest.WoolExists);
                })
                .OrderByDescending(r => r.Used)
                .ThenBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
                .Take(RankingSize)
                .ToList(),
            TopProjects = usedInPeriod
                .Where(m => m.ProjectId is not null || m.ProjectName is not null)
                .GroupBy(m => (m.ProjectId, m.ProjectName))
                .Select(group => new StatisticsProjectConsumption(
                    group.Key.ProjectId,
                    group.Key.ProjectName ?? string.Empty,
                    group.Sum(Amount),
                    group.Select(m => (m.WoolId, m.WoolName, m.WoolBrand)).Distinct().Count()))
                .OrderByDescending(p => p.Used)
                .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                .Take(RankingSize)
                .ToList()
        };
    }

    private static GlobalStatistics BuildGlobalStatistics(
        IReadOnlyList<Project> allProjects,
        IReadOnlyList<Pattern> allPatterns,
        int woolCount,
        int documentCount,
        StatisticsFilter filter)
    {
        var projects = allProjects
            .Where(p => filter.PatternType is null || p.Pattern?.Type == filter.PatternType)
            .ToList();
        var patterns = allPatterns
            .Where(p => filter.PatternType is null || p.Type == filter.PatternType)
            .ToList();

        var today = filter.Today;
        var earliest = projects
            .SelectMany(p => new[] { p.BeginDate, p.EndDate })
            .Where(date => date is not null && date <= today)
            .Select(date => date!.Value)
            .DefaultIfEmpty(today)
            .Min();
        var period = StatisticsPeriod.Create(filter.Range, today, earliest);

        var finished = projects
            .Where(p => p.Status == Status.Finished && p.EndDate is not null)
            .ToList();
        var finishedInPeriod = finished.Where(p => period.Contains(p.EndDate!.Value)).ToList();
        var started = projects.Where(p => p.BeginDate is not null).ToList();

        var durations = finishedInPeriod
            .Where(p => p.BeginDate is not null && p.BeginDate <= p.EndDate)
            .Select(p => (double)DurationDays(p.BeginDate!.Value, p.EndDate!.Value))
            .ToList();

        return new GlobalStatistics
        {
            Granularity = period.Granularity,
            TotalProjects = projects.Count,
            ProjectsInProgress = projects.Count(p => p.Status == Status.InProgress),
            ProjectsFinished = projects.Count(p => p.Status == Status.Finished),
            FinishedInPeriod = finishedInPeriod.Count,
            FinishedPreviousPeriod = period.PreviousStart is null
                ? null
                : finished.Count(p => period.ContainsPrevious(p.EndDate!.Value)),
            StartedInPeriod = started.Count(p => period.Contains(p.BeginDate!.Value)),
            AverageDurationDays = durations.Count == 0 ? null : durations.Average(),
            TotalPatterns = patterns.Count,
            PersonalPatterns = patterns.Count(p => p.IsPersonal),
            TotalWools = woolCount,
            TotalDocuments = documentCount,
            StartedTimeline = period.Timeline(started, p => p.BeginDate!.Value, _ => 1),
            FinishedTimeline = period.Timeline(finished, p => p.EndDate!.Value, _ => 1),
            ProjectsByStatus = Shares(projects, p => p.Status, _ => 1),
            ProjectsByPatternType = Shares(projects, p => p.Pattern?.Type, _ => 1),
            LongestProjects = projects
                .Where(p => p.BeginDate is not null && p.BeginDate <= today && p.Status != Status.Wishlist)
                .Select(p =>
                {
                    var isOngoing = p.Status != Status.Finished || p.EndDate is null;
                    var end = isOngoing ? today : p.EndDate!.Value;
                    return (Project: p, End: end, IsOngoing: isOngoing);
                })
                .Where(x => x.End >= x.Project.BeginDate && x.End >= period.Start)
                .Select(x => new StatisticsProjectDuration(
                    x.Project.ProjectId,
                    x.Project.Name,
                    x.Project.Status,
                    DurationDays(x.Project.BeginDate!.Value, x.End),
                    x.IsOngoing))
                .OrderByDescending(d => d.Days)
                .ThenBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)
                .Take(RankingSize)
                .ToList()
        };
    }

    private static IReadOnlyList<StatisticsShare<TKey>> Shares<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> key,
        Func<T, double> value) =>
        items
            .GroupBy(key)
            .Select(group => new StatisticsShare<TKey>(group.Key, group.Sum(value)))
            .Where(share => share.Value > 0)
            .OrderByDescending(share => share.Value)
            .ToList();

    private static DateOnly DateOf(TrackedWoolMovement movement) => DateOnly.FromDateTime(movement.Date);

    /// <summary>Nombre de jours calendaires, bornes incluses : un projet commencé et fini le même jour dure 1 jour.</summary>
    private static int DurationDays(DateOnly begin, DateOnly end) => end.DayNumber - begin.DayNumber + 1;

    private static WoolType? WeightClass(double needleMinSize, double needleMaxSize) =>
        Wool.FindContainingNeedleRange(needleMinSize, needleMaxSize)?.Type;

    private static string NormalizeMaterial(string material)
    {
        var trimmed = material.Trim();
        return trimmed.Length == 0 ? string.Empty : char.ToUpper(trimmed[0]) + trimmed[1..].ToLowerInvariant();
    }

    private static double Convert(double skeins, StatisticsQuantityUnit unit, double weightPerSkein, double lengthPerSkein) =>
        unit switch
        {
            StatisticsQuantityUnit.Weight => skeins * weightPerSkein,
            StatisticsQuantityUnit.Length => skeins * lengthPerSkein,
            _ => skeins
        };
}
