// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.IServices;
using Looma.Domain.Statistics;
using Looma.Presentation.Tests.TestSupport;
using Looma.Presentation.ViewModels.Sections.Statistics;

namespace Looma.Presentation.Tests.Sections.Statistics;

public sealed class StatisticsViewModelTests
{
    [Fact]
    public void OnNavigatedTo_Loads_Default_Data()
    {
        var service = new FakeStatisticsService
        {
            Result = ResultT<StatisticsSnapshot>.Ok(new StatisticsSnapshot(
                ["06/2026"],
                [new StatisticsSeries("Ajouts", [new StatisticsPoint("06/2026", new DateOnly(2026, 6, 1), 10)])],
                []))
        };
        var vm = CreateViewModel(service);

        vm.OnNavigatedTo();

        service.Queries.Should().ContainSingle();
        vm.HasData.Should().BeTrue();
        service.Queries.Single().ChartKind.Should().Be(StatisticsChartKind.Line);
        service.Queries.Single().DataKind.Should().Be(StatisticsDataKind.Wool);
        service.Queries.Single().Range.Should().Be(StatisticsRange.All);
        service.Queries.Single().QuantityUnit.Should().Be(StatisticsQuantityUnit.Skein);
        service.Queries.Single().PatternType.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_Reloads_Data()
    {
        var service = new FakeStatisticsService();
        var vm = CreateViewModel(service);
        vm.OnNavigatedTo();
        service.Queries.Clear();

        await vm.LoadAsync();

        service.Queries.Should().ContainSingle();
    }

    [Fact]
    public void Changing_Range_Reloads_Data()
    {
        var service = new FakeStatisticsService();
        var vm = CreateViewModel(service);
        vm.OnNavigatedTo();
        service.Queries.Clear();

        vm.SelectedRange = vm.RangeOptions.Single(option => option.Value == StatisticsRange.ThisMonth);

        service.Queries.Should().ContainSingle();
        service.Queries.Single().Range.Should().Be(StatisticsRange.ThisMonth);
    }

    [Fact]
    public void Changing_Pattern_Type_Reloads_Data()
    {
        var service = new FakeStatisticsService();
        var vm = CreateViewModel(service);
        vm.OnNavigatedTo();
        service.Queries.Clear();

        vm.SelectedPatternType = vm.PatternTypeOptions.Single(option => option.Value == PatternType.TunisianCrochet);

        service.Queries.Should().ContainSingle();
        service.Queries.Single().PatternType.Should().Be(PatternType.TunisianCrochet);
    }

    [Fact]
    public void Changing_Quantity_Unit_Reloads_Data()
    {
        var service = new FakeStatisticsService();
        var vm = CreateViewModel(service);
        vm.OnNavigatedTo();
        service.Queries.Clear();

        vm.SelectedQuantityUnit = vm.QuantityUnitOptions.Single(option => option.Value == StatisticsQuantityUnit.Length);

        service.Queries.Should().ContainSingle();
        service.Queries.Single().QuantityUnit.Should().Be(StatisticsQuantityUnit.Length);
    }

    [Fact]
    public void Empty_Result_Sets_Empty_State()
    {
        var service = new FakeStatisticsService
        {
            Result = ResultT<StatisticsSnapshot>.Ok(new StatisticsSnapshot([], [], []))
        };
        var vm = CreateViewModel(service);

        vm.OnNavigatedTo();

        vm.HasData.Should().BeFalse();
        vm.Series.Should().BeEmpty();
    }

    private static StatisticsViewModel CreateViewModel(FakeStatisticsService service) =>
        new(service, new FakeNotificationService(), new FakeRefreshService());

    private sealed class FakeStatisticsService : IStatisticsService
    {
        public List<StatisticsQuery> Queries { get; } = [];
        public ResultT<StatisticsSnapshot> Result { get; set; } =
            ResultT<StatisticsSnapshot>.Ok(new StatisticsSnapshot([], [], [new StatisticsSlice("A", 10)]));

        public Task<ResultT<StatisticsSnapshot>> GetAsync(StatisticsQuery query)
        {
            Queries.Add(query);
            return Task.FromResult(Result);
        }
    }
}
