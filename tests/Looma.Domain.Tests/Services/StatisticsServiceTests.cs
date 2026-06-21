// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Services;
using Looma.Domain.Statistics;
using FluentAssertions;

namespace Looma.Domain.Tests.Services;

public sealed class StatisticsServiceTests
{
    private static readonly DateOnly Today = new(2026, 6, 21);

    [Fact]
    public async Task Wool_Line_Shows_Cumulative_Stock_By_Wool()
    {
        var tracked = new FakeTrackedWoolRepository
        {
            Movements =
            [
                Movement("a", new DateTime(2026, 6, 2), 100, woolName: "Alpaca", woolBrand: "Drops", woolId: 1),
                Movement("b", new DateTime(2026, 6, 2), -40, woolName: "Alpaca", woolBrand: "Drops", woolId: 1),
                Movement("c", new DateTime(2026, 6, 3), -10, woolName: "Alpaca", woolBrand: "Drops", woolId: 1),
                Movement("d", new DateTime(2026, 6, 3), 250, woolName: "Cotton", woolBrand: "DMC", woolId: 2),
                Movement("e", new DateTime(2026, 6, 4), -50, woolName: "Cotton", woolBrand: "DMC", woolId: 2)
            ]
        };
        var service = new StatisticsService(tracked, new FakeProjectRepository());

        var result = await service.GetAsync(Query(StatisticsChartKind.Line, StatisticsDataKind.Wool, StatisticsRange.ThisMonth));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Series.Should().HaveCount(2);
        var alpaca = result.Value.Series.Single(s => s.Name == "Drops - Alpaca");
        alpaca.Points.Single(p => p.Date == new DateOnly(2026, 6, 2)).Value.Should().BeApproximately(0.06, 0.0001);
        alpaca.Points.Single(p => p.Date == new DateOnly(2026, 6, 3)).Value.Should().BeApproximately(0.05, 0.0001);

        var cotton = result.Value.Series.Single(s => s.Name == "DMC - Cotton");
        cotton.Points.Single(p => p.Date == new DateOnly(2026, 6, 3)).Value.Should().BeApproximately(0.25, 0.0001);
        cotton.Points.Single(p => p.Date == new DateOnly(2026, 6, 4)).Value.Should().BeApproximately(0.2, 0.0001);
    }

    [Fact]
    public async Task Wool_Pie_Uses_Negative_Movements_And_Includes_Adjustments_Without_Project()
    {
        var tracked = new FakeTrackedWoolRepository
        {
            Movements =
            [
                Movement("a", new DateTime(2026, 6, 2), -80, woolName: "Alpaca", woolBrand: "Drops", projectId: 1),
                Movement("b", new DateTime(2026, 6, 3), -20, woolName: "Alpaca", woolBrand: "Drops"),
                Movement("c", new DateTime(2026, 6, 4), 100, woolName: "Cotton", woolBrand: "DMC")
            ]
        };
        var service = new StatisticsService(tracked, new FakeProjectRepository());

        var result = await service.GetAsync(Query(StatisticsChartKind.Pie, StatisticsDataKind.Wool, StatisticsRange.ThisMonth));

        result.Value!.Slices.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new StatisticsSlice("Drops - Alpaca", 0.1));
    }

    [Fact]
    public async Task Project_Line_Shows_Cumulative_Wool_Usage_By_Project()
    {
        var tracked = new FakeTrackedWoolRepository
        {
            Movements =
            [
                Movement("a", new DateTime(2026, 1, 12), -80, projectId: 1, projectName: "Shawl", projectStatus: Status.InProgress, patternType: PatternType.Crochet),
                Movement("b", new DateTime(2026, 1, 20), -20, projectId: 1, projectName: "Shawl", projectStatus: Status.InProgress, patternType: PatternType.Crochet),
                Movement("c", new DateTime(2026, 2, 4), -50, projectId: 2, projectName: "Socks", projectStatus: Status.Finished, patternType: PatternType.Tricot),
                Movement("d", new DateTime(2026, 2, 5), -500)
            ]
        };
        var service = new StatisticsService(tracked, new FakeProjectRepository());

        var result = await service.GetAsync(Query(
            StatisticsChartKind.Line,
            StatisticsDataKind.Project,
            StatisticsRange.ThisYear));

        result.Value!.Series.Should().HaveCount(2);
        var shawl = result.Value.Series.Single(s => s.Name == "Shawl");
        shawl.Points.Single(p => p.Date == new DateOnly(2026, 1, 1)).Value.Should().BeApproximately(0.1, 0.0001);
        shawl.Points.Single(p => p.Date == new DateOnly(2026, 2, 1)).Value.Should().BeApproximately(0.1, 0.0001);

        var socks = result.Value.Series.Single(s => s.Name == "Socks");
        socks.Points.Single(p => p.Date == new DateOnly(2026, 2, 1)).Value.Should().BeApproximately(0.05, 0.0001);

        var filtered = await service.GetAsync(Query(
            StatisticsChartKind.Line,
            StatisticsDataKind.Project,
            StatisticsRange.ThisYear,
            projectStatus: Status.Finished));

        filtered.Value!.Series.Should().ContainSingle().Which.Name.Should().Be("Socks");
    }

    [Fact]
    public async Task Project_Pie_Counts_Projects_By_Status_Or_Pattern_Type()
    {
        var projects = new FakeProjectRepository
        {
            Projects =
            [
                Project(1, "Shawl", new DateOnly(2026, 1, 12), PatternType.Crochet, Status.InProgress),
                Project(2, "Socks", new DateOnly(2026, 2, 20), PatternType.Tricot, Status.Finished),
                Project(3, "Scarf", new DateOnly(2026, 3, 10), PatternType.Crochet, Status.Finished)
            ]
        };
        var service = new StatisticsService(new FakeTrackedWoolRepository(), projects);

        var byStatus = await service.GetAsync(Query(StatisticsChartKind.Pie, StatisticsDataKind.Project, StatisticsRange.ThisYear));
        var byType = await service.GetAsync(Query(
            StatisticsChartKind.Pie,
            StatisticsDataKind.Project,
            StatisticsRange.ThisYear,
            grouping: StatisticsProjectGrouping.PatternType));

        byStatus.Value!.Slices.Should().BeEquivalentTo(
            [
                new StatisticsSlice("Terminé", 2),
                new StatisticsSlice("En cours", 1)
            ]);
        byType.Value!.Slices.Should().BeEquivalentTo(
            [
                new StatisticsSlice("Crochet", 2),
                new StatisticsSlice("Tricot", 1)
            ]);
    }

    [Fact]
    public async Task Range_Is_Passed_To_Tracked_Wool_Repository()
    {
        var tracked = new FakeTrackedWoolRepository();
        var service = new StatisticsService(tracked, new FakeProjectRepository());

        await service.GetAsync(Query(StatisticsChartKind.Pie, StatisticsDataKind.Wool, StatisticsRange.ThisYear));

        tracked.LastFrom.Should().Be(new DateTime(2026, 1, 1));
    }

    private static StatisticsQuery Query(
        StatisticsChartKind chartKind,
        StatisticsDataKind dataKind,
        StatisticsRange range,
        PatternType? patternType = null,
        Status? projectStatus = null,
        StatisticsProjectGrouping grouping = StatisticsProjectGrouping.Status) =>
        new(chartKind, dataKind, range, patternType, projectStatus, grouping, Today);

    private static TrackedWoolMovement Movement(
        string id,
        DateTime date,
        double quantity,
        string woolName = "Alpaca",
        string woolBrand = "Drops",
        int woolId = 1,
        int? projectId = null,
        string? projectName = null,
        Status? projectStatus = null,
        PatternType? patternType = null) =>
        new(id, date, quantity, woolId, woolName, woolBrand, projectId, projectName, projectStatus, patternType);

    private static Project Project(
        int id,
        string name,
        DateOnly beginDate,
        PatternType patternType,
        Status status = Status.InProgress) =>
        new()
        {
            ProjectId = id,
            Name = name,
            Status = status,
            Pattern = new Pattern
            {
                Id = id,
                Name = $"{name} pattern",
                IsPersonal = false,
                Url = null,
                Note = null,
                BeginDate = null,
                EndDate = null,
                Type = patternType,
                Documents = [],
                Projects = []
            },
            Note = null,
            BeginDate = beginDate,
            EndDate = null,
            Wools = [],
            Files = []
        };

    private sealed class FakeTrackedWoolRepository : ITrackedWoolRepository
    {
        public IReadOnlyList<TrackedWoolMovement> Movements { get; init; } = [];
        public DateTime? LastFrom { get; private set; }

        public Task<Result> AddAsync(int woolId, double quantity, int? projectId = null, DateTime? date = null) => Task.FromResult(Result.Ok());

        public Task<ResultT<IReadOnlyList<TrackedWoolMovement>>> GetMovementsAsync(DateTime? from = null)
        {
            LastFrom = from;
            return Task.FromResult(ResultT<IReadOnlyList<TrackedWoolMovement>>.Ok(Movements));
        }
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        public IReadOnlyList<Project> Projects { get; init; } = [];

        public Task<ResultT<IReadOnlyList<Project>>> GetAllAsync() =>
            Task.FromResult(ResultT<IReadOnlyList<Project>>.Ok(Projects));

        public Task<ResultT<Project>> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<ResultT<Project>> AddAsync(Looma.Domain.Request.CreateProjectRequest request) => throw new NotImplementedException();
        public Task<ResultT<Project>> UpdateAsync(Looma.Domain.Request.UpdateProjectRequest request) => throw new NotImplementedException();
        public Task<Result> DeleteAsync(int id) => throw new NotImplementedException();
    }
}
