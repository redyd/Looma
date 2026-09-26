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
    public void OnNavigatedTo_Loads_Wool_Tab_With_Default_Filter()
    {
        var service = new FakeStatisticsService();
        var vm = CreateViewModel(service);

        vm.OnNavigatedTo();

        vm.IsWoolTab.Should().BeTrue();
        service.WoolFilters.Should().ContainSingle();
        service.GlobalFilters.Should().BeEmpty();
        var filter = service.WoolFilters.Single();
        filter.Range.Should().Be(StatisticsRange.All);
        filter.Unit.Should().Be(StatisticsQuantityUnit.Skein);
        filter.PatternType.Should().BeNull();
        vm.HasData.Should().BeTrue();
        vm.WoolReport.Kpis.Should().HaveCount(4);
        vm.WoolReport.Timeline.Series.Should().HaveCount(2);
        vm.WoolReport.TopWools.Should().ContainSingle().Which.HasBadge.Should().BeTrue();
    }

    [Fact]
    public void Selecting_Global_Tab_Loads_Global_Statistics()
    {
        var service = new FakeStatisticsService();
        var vm = CreateViewModel(service);
        vm.OnNavigatedTo();

        vm.SelectGlobalTabCommand.Execute(null);

        vm.IsGlobalTab.Should().BeTrue();
        service.GlobalFilters.Should().ContainSingle();
        vm.GlobalReport.Kpis.Should().HaveCount(4);
        vm.GlobalReport.ByStatus.Slices.Should().ContainSingle();
        vm.HasData.Should().BeTrue();
    }

    [Fact]
    public void Changing_Filters_Reloads_Active_Tab()
    {
        var service = new FakeStatisticsService();
        var vm = CreateViewModel(service);
        vm.OnNavigatedTo();
        service.WoolFilters.Clear();

        vm.SelectedRange = vm.RangeOptions.Single(option => option.Value == StatisticsRange.ThisMonth);
        vm.SelectedQuantityUnit = vm.QuantityUnitOptions.Single(option => option.Value == StatisticsQuantityUnit.Length);
        vm.SelectedPatternType = vm.PatternTypeOptions.Single(option => option.Value == PatternType.TunisianCrochet);

        service.WoolFilters.Should().HaveCount(3);
        var last = service.WoolFilters.Last();
        last.Range.Should().Be(StatisticsRange.ThisMonth);
        last.Unit.Should().Be(StatisticsQuantityUnit.Length);
        last.PatternType.Should().Be(PatternType.TunisianCrochet);
    }

    [Fact]
    public async Task LoadAsync_Reloads_Data()
    {
        var service = new FakeStatisticsService();
        var vm = CreateViewModel(service);
        vm.OnNavigatedTo();
        service.WoolFilters.Clear();

        await vm.LoadAsync();

        service.WoolFilters.Should().ContainSingle();
    }

    [Fact]
    public void Failure_Notifies_And_Shows_Empty_State()
    {
        var notifications = new FakeNotificationService();
        var service = new FakeStatisticsService
        {
            WoolResult = ResultT<WoolStatistics>.Failure("boom")
        };
        var vm = new StatisticsViewModel(service, notifications, new FakeRefreshService());

        vm.OnNavigatedTo();

        vm.HasData.Should().BeFalse();
        vm.WoolReport.Kpis.Should().BeEmpty();
        notifications.Calls.Should().Contain(call => call.Message == "boom");
    }

    [Fact]
    public void Donut_Groups_Small_Shares_Into_Others()
    {
        var shares = Enumerable.Range(1, 8)
            .Select(i => new StatisticsShare<string>($"M{i}", 10 - i))
            .ToList();

        var donut = StatisticsFormatter.Donut(
            shares,
            key => key,
            (_, index) => StatisticsFormatter.SeriesBrush(index),
            value => value.ToString(),
            Looma.Presentation.Services.TranslationService.Current);

        donut.Slices.Should().HaveCount(6);
        donut.Slices.Last().Value.Should().Be(4 + 3 + 2);
        donut.Slices.Sum(s => s.Percent).Should().BeApproximately(1, 0.0001);
    }

    private static StatisticsViewModel CreateViewModel(FakeStatisticsService service) =>
        new(service, new FakeNotificationService(), new FakeRefreshService());

    private sealed class FakeStatisticsService : IStatisticsService
    {
        public List<StatisticsFilter> WoolFilters { get; } = [];
        public List<StatisticsFilter> GlobalFilters { get; } = [];

        public ResultT<WoolStatistics> WoolResult { get; init; } = ResultT<WoolStatistics>.Ok(new WoolStatistics
        {
            Granularity = StatisticsGranularity.Month,
            Unit = StatisticsQuantityUnit.Skein,
            Used = 3,
            Added = 5,
            UsedPreviousPeriod = null,
            CurrentStock = 12,
            WoolsInStock = 4,
            ProjectsSupplied = 1,
            UsedTimeline = [new(new DateOnly(2026, 6, 1), 3)],
            AddedTimeline = [new(new DateOnly(2026, 6, 1), 5)],
            UsedByMaterial = [new("Laine", 3)],
            UsedByWeightClass = [new(WoolType.Medium, 3)],
            StockByWeightClass = [new(WoolType.Medium, 12)],
            TopWools = [new(null, "Mohair", "Archive", ["#FFFFFF"], 3, false)],
            TopProjects = [new(1, "Pull", 3, 1)]
        });

        public ResultT<GlobalStatistics> GlobalResult { get; init; } = ResultT<GlobalStatistics>.Ok(new GlobalStatistics
        {
            Granularity = StatisticsGranularity.Month,
            TotalProjects = 2,
            ProjectsInProgress = 1,
            ProjectsFinished = 1,
            FinishedInPeriod = 1,
            FinishedPreviousPeriod = null,
            StartedInPeriod = 2,
            AverageDurationDays = 12,
            TotalPatterns = 1,
            PersonalPatterns = 0,
            TotalWools = 4,
            TotalDocuments = 0,
            StartedTimeline = [new(new DateOnly(2026, 6, 1), 2)],
            FinishedTimeline = [new(new DateOnly(2026, 6, 1), 1)],
            ProjectsByStatus = [new(Status.InProgress, 2)],
            ProjectsByPatternType = [new(null, 2)],
            LongestProjects = [new(1, "Pull", Status.InProgress, 30, true)]
        });

        public Task<ResultT<WoolStatistics>> GetWoolStatisticsAsync(StatisticsFilter filter)
        {
            WoolFilters.Add(filter);
            return Task.FromResult(WoolResult);
        }

        public Task<ResultT<GlobalStatistics>> GetGlobalStatisticsAsync(StatisticsFilter filter)
        {
            GlobalFilters.Add(filter);
            return Task.FromResult(GlobalResult);
        }
    }
}
