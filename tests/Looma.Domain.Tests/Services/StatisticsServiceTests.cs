// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Services;
using Looma.Domain.Statistics;
using NSubstitute;

namespace Looma.Domain.Tests.Services;

public sealed class StatisticsServiceTests
{
    // Dimanche 21 juin 2026.
    private static readonly DateOnly Today = new(2026, 6, 21);

    private readonly ITrackedWoolRepository _tracked = Substitute.For<ITrackedWoolRepository>();
    private readonly IWoolRepository _wools = Substitute.For<IWoolRepository>();
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IPatternRepository _patterns = Substitute.For<IPatternRepository>();
    private readonly IDocumentRepository _documents = Substitute.For<IDocumentRepository>();

    public StatisticsServiceTests()
    {
        GivenMovements();
        GivenWools();
        GivenProjects();
        _patterns.GetAllAsync().Returns(ResultT<IReadOnlyList<Pattern>>.Ok([]));
        _documents.GetAllAsync().Returns(ResultT<IReadOnlyList<Document>>.Ok([]));
    }

    [Fact]
    public async Task Wool_Timeline_Is_Per_Bucket_Not_Cumulative()
    {
        GivenMovements(
            Movement(new DateTime(2026, 4, 10), -2_000),
            Movement(new DateTime(2026, 5, 5), -500),
            Movement(new DateTime(2026, 5, 20), -500),
            Movement(new DateTime(2026, 6, 3), 1_000));

        var stats = await WoolStats(StatisticsRange.ThisYear);

        stats.Granularity.Should().Be(StatisticsGranularity.Month);
        stats.UsedTimeline.Select(b => b.Value).Should().Equal(0, 0, 0, 2, 1, 0);
        stats.AddedTimeline.Select(b => b.Value).Should().Equal(0, 0, 0, 0, 0, 1);
        stats.Used.Should().Be(3);
        stats.Added.Should().Be(1);
    }

    [Theory]
    [InlineData(StatisticsRange.ThisWeek, StatisticsGranularity.Day, 7)]
    [InlineData(StatisticsRange.ThisMonth, StatisticsGranularity.Day, 21)]
    [InlineData(StatisticsRange.LastSixMonths, StatisticsGranularity.Week, 26)]
    [InlineData(StatisticsRange.ThisYear, StatisticsGranularity.Month, 6)]
    public async Task Range_Defines_Granularity_And_Buckets(StatisticsRange range, StatisticsGranularity granularity, int buckets)
    {
        GivenMovements(Movement(new DateTime(2026, 6, 20), -1_000));

        var stats = await WoolStats(range);

        stats.Granularity.Should().Be(granularity);
        stats.UsedTimeline.Should().HaveCount(buckets);
        stats.UsedTimeline.Sum(b => b.Value).Should().Be(1);
    }

    [Fact]
    public async Task All_Range_Starts_At_First_Movement()
    {
        GivenMovements(Movement(new DateTime(2025, 1, 15), -1_000));

        var stats = await WoolStats(StatisticsRange.All);

        stats.Granularity.Should().Be(StatisticsGranularity.Month);
        stats.UsedTimeline.First().Start.Should().Be(new DateOnly(2025, 1, 1));
        stats.UsedTimeline.Last().Start.Should().Be(new DateOnly(2026, 6, 1));
        stats.UsedPreviousPeriod.Should().BeNull();
    }

    [Fact]
    public async Task Converts_Using_Snapshot_Weight_And_Length()
    {
        GivenMovements(Movement(new DateTime(2026, 6, 1), -2_000, weight: 50, length: 100));

        (await WoolStats(StatisticsRange.ThisYear, StatisticsQuantityUnit.Weight)).Used.Should().Be(100);
        (await WoolStats(StatisticsRange.ThisYear, StatisticsQuantityUnit.Length)).Used.Should().Be(200);
    }

    [Fact]
    public async Task Pattern_Type_Filter_Applies_To_Consumption_Only()
    {
        GivenMovements(
            Movement(new DateTime(2026, 6, 1), -1_000, patternType: PatternType.Crochet),
            Movement(new DateTime(2026, 6, 2), -3_000, patternType: PatternType.Tricot),
            Movement(new DateTime(2026, 6, 3), 2_000, projectId: null));

        var stats = await WoolStats(StatisticsRange.ThisMonth, patternType: PatternType.Crochet);

        stats.Used.Should().Be(1);
        stats.Added.Should().Be(2);
    }

    [Fact]
    public async Task Compares_With_Previous_Period_Of_Same_Length()
    {
        // Semaine courante : 15 → 21 juin ; précédente : 8 → 14 juin.
        GivenMovements(
            Movement(new DateTime(2026, 6, 10), -1_000),
            Movement(new DateTime(2026, 6, 16), -3_000),
            Movement(new DateTime(2026, 5, 1), -9_000));

        var stats = await WoolStats(StatisticsRange.ThisWeek);

        stats.Used.Should().Be(3);
        stats.UsedPreviousPeriod.Should().Be(1);
        stats.UsedTrendPercent.Should().Be(200);
    }

    [Fact]
    public async Task Rankings_Include_Deleted_Wools_And_Projects()
    {
        GivenMovements(
            Movement(new DateTime(2026, 6, 1), -1_000, woolId: 1, name: "Alpaca", projectId: 1, projectName: "Pull"),
            Movement(new DateTime(2026, 6, 2), -4_000, woolId: null, name: "Mohair", projectId: null, projectName: "Châle"),
            Movement(new DateTime(2026, 6, 3), -2_000, woolId: 1, name: "Alpaca", projectId: 1, projectName: "Pull"));

        var stats = await WoolStats(StatisticsRange.ThisMonth);

        stats.TopWools.Select(w => (w.Name, w.Used, w.Exists)).Should().Equal(("Mohair", 4d, false), ("Alpaca", 3d, true));
        stats.TopProjects.Select(p => (p.Name, p.Used)).Should().Equal(("Châle", 4d), ("Pull", 3d));
        stats.ProjectsSupplied.Should().Be(2);
    }

    [Fact]
    public async Task Groups_Consumption_By_Material_And_Weight_Class()
    {
        GivenMovements(
            Movement(new DateTime(2026, 6, 1), -1_000, material: "merinos", needles: (3.25, 3.75)),
            Movement(new DateTime(2026, 6, 2), -2_000, material: "Coton ", needles: (5, 5.75)),
            Movement(new DateTime(2026, 6, 3), -1_000, material: "Merinos", needles: (3.25, 3.75)));

        var stats = await WoolStats(StatisticsRange.ThisMonth);

        stats.UsedByMaterial.Select(s => (s.Key, s.Value)).Should().BeEquivalentTo([("Coton", 2d), ("Merinos", 2d)]);
        stats.UsedByWeightClass.Select(s => (s.Key, s.Value)).Should().BeEquivalentTo([(WoolType.Medium, 2d), (WoolType.Fine, 2d)]);
    }

    [Fact]
    public async Task Current_Stock_Uses_Wools_In_Stock()
    {
        GivenWools(Wool(1, stock: 2_500, weight: 50), Wool(2, stock: 0, weight: 100));

        var stats = await WoolStats(StatisticsRange.All, StatisticsQuantityUnit.Weight);

        stats.CurrentStock.Should().Be(125);
        stats.WoolsInStock.Should().Be(1);
        stats.StockByWeightClass.Should().ContainSingle().Which.Value.Should().Be(125);
    }

    [Fact]
    public async Task Global_Counts_Projects_Durations_And_Timelines()
    {
        GivenProjects(
            Project(1, Status.Finished, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 10), PatternType.Crochet),
            Project(2, Status.Finished, new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 2), PatternType.Tricot),
            Project(3, Status.InProgress, new DateOnly(2026, 1, 10), null, null),
            Project(4, Status.Wishlist, null, null, PatternType.Crochet),
            Project(5, Status.Finished, new DateOnly(2025, 3, 1), new DateOnly(2025, 12, 1), null));

        var stats = await GlobalStats(StatisticsRange.ThisYear);

        stats.TotalProjects.Should().Be(5);
        stats.ProjectsInProgress.Should().Be(1);
        stats.FinishedInPeriod.Should().Be(2);
        stats.StartedInPeriod.Should().Be(3);
        stats.AverageDurationDays.Should().Be((10 + 30) / 2d);
        stats.FinishedTimeline.Select(b => b.Value).Should().Equal(0, 0, 1, 0, 1, 0);
        stats.ProjectsByStatus.Should().Contain(s => s.Key == Status.Finished && s.Value == 3);
        stats.ProjectsByPatternType.Should().Contain(s => s.Key == null && s.Value == 2);
        stats.LongestProjects.First().Should().Match<StatisticsProjectDuration>(d => d.ProjectId == 3 && d.IsOngoing && d.Days == 163);
        stats.LongestProjects.Should().NotContain(d => d.ProjectId == 5);
    }

    [Fact]
    public async Task Global_Pattern_Type_Filter_Restricts_Projects()
    {
        GivenProjects(
            Project(1, Status.InProgress, new DateOnly(2026, 5, 1), null, PatternType.Crochet),
            Project(2, Status.InProgress, new DateOnly(2026, 5, 1), null, PatternType.Tricot));

        var stats = await GlobalStats(StatisticsRange.All, PatternType.Tricot);

        stats.TotalProjects.Should().Be(1);
    }

    [Fact]
    public async Task Repository_Failure_Is_Returned()
    {
        _tracked.GetMovementsAsync().Returns(ResultT<IReadOnlyList<TrackedWoolMovement>>.Failure("boom"));

        var result = await Service().GetWoolStatisticsAsync(Filter(StatisticsRange.All));

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("boom");
    }

    private StatisticsService Service() => new(_tracked, _wools, _projects, _patterns, _documents);

    private async Task<WoolStatistics> WoolStats(
        StatisticsRange range,
        StatisticsQuantityUnit unit = StatisticsQuantityUnit.Skein,
        PatternType? patternType = null)
    {
        var result = await Service().GetWoolStatisticsAsync(Filter(range, unit, patternType));
        result.Succeeded.Should().BeTrue(result.Error);
        return result.Value!;
    }

    private async Task<GlobalStatistics> GlobalStats(StatisticsRange range, PatternType? patternType = null)
    {
        var result = await Service().GetGlobalStatisticsAsync(Filter(range, patternType: patternType));
        result.Succeeded.Should().BeTrue(result.Error);
        return result.Value!;
    }

    private static StatisticsFilter Filter(
        StatisticsRange range,
        StatisticsQuantityUnit unit = StatisticsQuantityUnit.Skein,
        PatternType? patternType = null) =>
        new(range, unit, patternType, Today);

    private void GivenMovements(params TrackedWoolMovement[] movements) =>
        _tracked.GetMovementsAsync().Returns(ResultT<IReadOnlyList<TrackedWoolMovement>>.Ok(movements));

    private void GivenWools(params Wool[] wools) =>
        _wools.GetAllAsync().Returns(ResultT<IReadOnlyList<Wool>>.Ok(wools));

    private void GivenProjects(params Project[] projects) =>
        _projects.GetAllAsync().Returns(ResultT<IReadOnlyList<Project>>.Ok(projects));

    private static TrackedWoolMovement Movement(
        DateTime date,
        double quantity,
        int? woolId = 1,
        string name = "Alpaca",
        int? projectId = 1,
        string? projectName = "Projet",
        PatternType? patternType = PatternType.Crochet,
        double weight = 50,
        double length = 100,
        string material = "Laine",
        (double Min, double Max)? needles = null) =>
        new(Guid.NewGuid().ToString("N"), date, quantity, woolId, name, "Drops", projectId, projectName,
            projectId is null ? null : Status.InProgress, projectId is null && quantity > 0 ? null : patternType)
        {
            WoolMaterial = material,
            WoolColors = ["#FFFFFF"],
            WoolWeight = weight,
            WoolLength = length,
            WoolNeedleMinSize = needles?.Min ?? 4,
            WoolNeedleMaxSize = needles?.Max ?? 4.75
        };

    private static Wool Wool(int id, double stock, double weight) =>
        new()
        {
            Id = id,
            Name = $"Laine {id}",
            Brand = "Drops",
            Material = "Laine",
            Colors = ["Bleu"],
            Weight = weight,
            Length = 100,
            Stock = stock,
            NeedleMinSize = 4,
            NeedleMaxSize = 4.75
        };

    private static Project Project(int id, Status status, DateOnly? begin, DateOnly? end, PatternType? type) =>
        new()
        {
            ProjectId = id,
            Name = $"Projet {id}",
            Status = status,
            Note = null,
            BeginDate = begin,
            EndDate = end,
            Pattern = type is null
                ? null
                : new Pattern
                {
                    Id = id,
                    Name = $"Patron {id}",
                    IsPersonal = false,
                    Url = null,
                    Note = null,
                    BeginDate = null,
                    EndDate = null,
                    Type = type.Value,
                    Documents = [],
                    Projects = []
                }
        };
}
