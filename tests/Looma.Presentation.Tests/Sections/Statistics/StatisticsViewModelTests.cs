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
        vm.IsLineChart.Should().BeTrue();
        vm.ShowPatternTypeFilter.Should().BeFalse();
        vm.ShowProjectGroupingFilter.Should().BeFalse();
    }

    [Fact]
    public void Changing_Filters_Reloads_Data_And_Updates_Visibility()
    {
        var service = new FakeStatisticsService();
        var vm = CreateViewModel(service);
        vm.OnNavigatedTo();
        service.Queries.Clear();

        vm.SelectedData = vm.DataOptions.Single(option => option.Value == StatisticsDataKind.Project);
        vm.SelectedChart = vm.ChartOptions.Single(option => option.Value == StatisticsChartKind.Pie);
        vm.SelectedProjectStatus = vm.ProjectStatusOptions.Single(option => option.Value == Status.Finished);

        service.Queries.Should().HaveCount(3);
        vm.ShowPatternTypeFilter.Should().BeTrue();
        vm.ShowProjectGroupingFilter.Should().BeTrue();
        service.Queries.Last().DataKind.Should().Be(StatisticsDataKind.Project);
        service.Queries.Last().ChartKind.Should().Be(StatisticsChartKind.Pie);
        service.Queries.Last().ProjectStatus.Should().Be(Status.Finished);
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
        vm.Slices.Should().BeEmpty();
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
