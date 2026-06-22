// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Request;
using Looma.Domain.Services;
using Looma.Domain.Statistics;

namespace Looma.Domain.Tests.Services;

public sealed class StatisticsServiceTests
{
    private static readonly DateOnly Today = new(2026, 6, 21);

    [Fact]
    public async Task GetAsync_Shows_Cumulative_Used_Wool_By_Wool()
    {
        var tracked = new FakeTrackedWoolRepository
        {
            Movements =
            [
                Movement("a", new DateTime(2026, 4, 10), -2_000, woolId: 1, woolName: "Alpaca", woolBrand: "Drops", patternType: PatternType.Crochet),
                Movement("b", new DateTime(2026, 5, 5), -500, woolId: 2, woolName: "Cotton", woolBrand: "DMC", patternType: PatternType.Tricot),
                Movement("c", new DateTime(2026, 6, 3), 1_000, woolId: 1, woolName: "Alpaca", woolBrand: "Drops", patternType: PatternType.Crochet)
            ]
        };
        var service = new StatisticsService(tracked, new FakeWoolRepository
        {
            Wools =
            [
                Wool(1, "Alpaca", "Drops", weight: 50, length: 100),
                Wool(2, "Cotton", "DMC", weight: 100, length: 250)
            ]
        });

        var result = await service.GetAsync(Query());

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Labels.Should().Equal("04/2026", "05/2026", "06/2026");
        var alpaca = result.Value.Series.Single(s => s.Name == "Drops - Alpaca");
        alpaca.Points.Select(p => p.Value).Should().Equal(2.0, 2.0, 2.0);

        var cotton = result.Value.Series.Single(s => s.Name == "DMC - Cotton");
        cotton.Points.Select(p => p.Value).Should().Equal(0, 0.5, 0.5);
    }

    [Fact]
    public async Task GetAsync_Filters_Used_Wool_By_Pattern_Type()
    {
        var service = new StatisticsService(
            new FakeTrackedWoolRepository
            {
                Movements =
                [
                    Movement("a", new DateTime(2026, 4, 10), -2_000, woolId: 1, woolName: "Alpaca", woolBrand: "Drops", patternType: PatternType.Crochet),
                    Movement("b", new DateTime(2026, 5, 5), -500, woolId: 1, woolName: "Alpaca", woolBrand: "Drops", patternType: PatternType.Tricot)
                ]
            },
            new FakeWoolRepository
            {
                Wools = [Wool(1, "Alpaca", "Drops", weight: 50, length: 100)]
            });

        var result = await service.GetAsync(Query(patternType: PatternType.Tricot));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Labels.Should().Equal("05/2026", "06/2026");
        result.Value.Series.Should().ContainSingle()
            .Which.Name.Should().Be("Drops - Alpaca");
        result.Value.Series.Single().Points
            .Select(p => p.Value).Should().Equal(0.5, 0.5);
    }

    [Fact]
    public async Task GetAsync_Converts_Used_Wool_To_Selected_Unit()
    {
        var service = new StatisticsService(
            new FakeTrackedWoolRepository
            {
                Movements =
                [
                    Movement("a", new DateTime(2026, 6, 10), -2_000, woolId: 1, woolName: "Alpaca", woolBrand: "Drops", patternType: PatternType.Crochet)
                ]
            },
            new FakeWoolRepository
            {
                Wools = [Wool(1, "Alpaca", "Drops", weight: 50, length: 100)]
            });

        var weight = await service.GetAsync(Query(quantityUnit: StatisticsQuantityUnit.Weight));
        var length = await service.GetAsync(Query(quantityUnit: StatisticsQuantityUnit.Length));

        weight.Value!.Series.Single().Points.Last().Value.Should().Be(100);
        length.Value!.Series.Single().Points.Last().Value.Should().Be(200);
    }

    private static StatisticsQuery Query(
        PatternType? patternType = null,
        StatisticsQuantityUnit quantityUnit = StatisticsQuantityUnit.Skein) =>
        new(
            StatisticsChartKind.Line,
            StatisticsDataKind.Wool,
            StatisticsRange.All,
            patternType,
            null,
            StatisticsProjectGrouping.Status,
            quantityUnit,
            Today);

    private static TrackedWoolMovement Movement(
        string id,
        DateTime date,
        double quantity,
        int woolId,
        string woolName,
        string woolBrand,
        PatternType patternType) =>
        new(id, date, quantity, woolId, woolName, woolBrand, 1, "Projet", Status.InProgress, patternType);

    private static Wool Wool(int id, string name, string brand, double weight, double length) =>
        new()
        {
            Id = id,
            Name = name,
            Brand = brand,
            Material = "Laine",
            Colors = ["Bleu"],
            Weight = weight,
            Length = length,
            Stock = 0,
            NeedleMinSize = 4,
            NeedleMaxSize = 4.5
        };

    private sealed class FakeTrackedWoolRepository : ITrackedWoolRepository
    {
        public IReadOnlyList<TrackedWoolMovement> Movements { get; init; } = [];

        public Task<Result> AddAsync(int woolId, double quantity, int? projectId = null, DateTime? date = null) =>
            Task.FromResult(Result.Ok());

        public Task<ResultT<IReadOnlyList<TrackedWoolMovement>>> GetMovementsAsync(DateTime? from = null) =>
            Task.FromResult(ResultT<IReadOnlyList<TrackedWoolMovement>>.Ok(Movements));
    }

    private sealed class FakeWoolRepository : IWoolRepository
    {
        public IReadOnlyList<Wool> Wools { get; init; } = [];

        public Task<ResultT<IReadOnlyList<Wool>>> GetAllAsync() =>
            Task.FromResult(ResultT<IReadOnlyList<Wool>>.Ok(Wools));

        public Task<ResultT<Wool>> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<ResultT<Wool>> AddAsync(CreateWoolRequest request) => throw new NotImplementedException();
        public Task<ResultT<Wool>> UpdateAsync(UpdateWoolRequest request) => throw new NotImplementedException();
        public Task<Result> DeleteAsync(int id) => throw new NotImplementedException();
        public Task<Result> AddStock(int id, double quantity) => throw new NotImplementedException();
    }
}
