// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
using Looma.Domain.Extensions;
using Looma.Domain.IServices;
using Looma.Domain.Refresh;
using Looma.Domain.Statistics;
using Looma.Presentation.Notifications;
using Looma.Presentation.ViewModels.Base;

namespace Looma.Presentation.ViewModels.Sections.Statistics;

public partial class StatisticsViewModel(
    IStatisticsService statisticsService,
    INotificationService notifications,
    IDataRefreshService refreshService)
    : PageViewModelBase
{
    private bool _isInitialized;

    public IReadOnlyList<StatisticsOptionViewModel<StatisticsChartKind>> ChartOptions { get; } =
    [
        new("Courbe", StatisticsChartKind.Line),
        new("Camembert", StatisticsChartKind.Pie)
    ];

    public IReadOnlyList<StatisticsOptionViewModel<StatisticsDataKind>> DataOptions { get; } =
    [
        new("Laine", StatisticsDataKind.Wool),
        new("Projet", StatisticsDataKind.Project)
    ];

    public IReadOnlyList<StatisticsOptionViewModel<StatisticsRange>> RangeOptions { get; } =
    [
        new("Tout", StatisticsRange.All),
        new("Cette année", StatisticsRange.ThisYear),
        new("Ces 6 derniers mois", StatisticsRange.LastSixMonths),
        new("Ce mois", StatisticsRange.ThisMonth)
    ];

    public IReadOnlyList<StatisticsOptionViewModel<PatternType?>> PatternTypeOptions { get; } =
    [
        new("Tout", null),
        ..Enum.GetValues<PatternType>()
            .Select(type => new StatisticsOptionViewModel<PatternType?>(type.GetDisplayName(), type))
    ];

    public IReadOnlyList<StatisticsOptionViewModel<Status?>> ProjectStatusOptions { get; } =
    [
        new("Tout", null),
        ..Enum.GetValues<Status>()
            .Select(status => new StatisticsOptionViewModel<Status?>(status.GetDisplayName(), status))
    ];

    public IReadOnlyList<StatisticsOptionViewModel<StatisticsProjectGrouping>> ProjectGroupingOptions { get; } =
    [
        new("Statut", StatisticsProjectGrouping.Status),
        new("Type de projet", StatisticsProjectGrouping.PatternType)
    ];

    [ObservableProperty]
    public partial StatisticsOptionViewModel<StatisticsChartKind>? SelectedChart { get; set; }

    [ObservableProperty]
    public partial StatisticsOptionViewModel<StatisticsDataKind>? SelectedData { get; set; }

    [ObservableProperty]
    public partial StatisticsOptionViewModel<StatisticsRange>? SelectedRange { get; set; }

    [ObservableProperty]
    public partial StatisticsOptionViewModel<PatternType?>? SelectedPatternType { get; set; }

    [ObservableProperty]
    public partial StatisticsOptionViewModel<Status?>? SelectedProjectStatus { get; set; }

    [ObservableProperty]
    public partial StatisticsOptionViewModel<StatisticsProjectGrouping>? SelectedProjectGrouping { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> Labels { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<StatisticsSeries> Series { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<StatisticsSlice> Slices { get; set; } = [];

    [ObservableProperty]
    public partial bool HasData { get; set; }

    public bool IsLineChart => SelectedChart?.Value == StatisticsChartKind.Line;
    public bool IsPieChart => SelectedChart?.Value == StatisticsChartKind.Pie;
    public bool IsProjectData => SelectedData?.Value == StatisticsDataKind.Project;
    public bool ShowPatternTypeFilter => IsProjectData;
    public bool ShowProjectGroupingFilter => IsProjectData && IsPieChart;

    public override async void OnNavigatedTo()
    {
        RegisterRefresh(refreshService, RefreshScope.All, LoadAsync);

        if (!_isInitialized)
        {
            Title = "Statistiques";
            SelectedChart = ChartOptions.First();
            SelectedData = DataOptions.First();
            SelectedRange = RangeOptions.First();
            SelectedPatternType = PatternTypeOptions.First();
            SelectedProjectStatus = ProjectStatusOptions.First();
            SelectedProjectGrouping = ProjectGroupingOptions.First();
            _isInitialized = true;
        }

        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!_isInitialized
            || SelectedChart is null
            || SelectedData is null
            || SelectedRange is null
            || SelectedProjectGrouping is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var query = new StatisticsQuery(
                SelectedChart.Value,
                SelectedData.Value,
                SelectedRange.Value,
                SelectedData.Value == StatisticsDataKind.Project ? SelectedPatternType?.Value : null,
                SelectedData.Value == StatisticsDataKind.Project ? SelectedProjectStatus?.Value : null,
                SelectedProjectGrouping.Value,
                DateOnly.FromDateTime(DateTime.Today));

            var result = await statisticsService.GetAsync(query);
            if (result.Failed || result.Value is null)
            {
                notifications.Error(result.Error ?? "Impossible de charger les statistiques.");
                Labels = [];
                Series = [];
                Slices = [];
                HasData = false;
                return;
            }

            Labels = result.Value.Labels;
            Series = result.Value.Series;
            Slices = result.Value.Slices;
            HasData = !result.Value.IsEmpty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedChartChanged(StatisticsOptionViewModel<StatisticsChartKind>? value) => FilterChanged();
    partial void OnSelectedDataChanged(StatisticsOptionViewModel<StatisticsDataKind>? value) => FilterChanged();
    partial void OnSelectedRangeChanged(StatisticsOptionViewModel<StatisticsRange>? value) => FilterChanged();
    partial void OnSelectedPatternTypeChanged(StatisticsOptionViewModel<PatternType?>? value) => FilterChanged();
    partial void OnSelectedProjectStatusChanged(StatisticsOptionViewModel<Status?>? value) => FilterChanged();
    partial void OnSelectedProjectGroupingChanged(StatisticsOptionViewModel<StatisticsProjectGrouping>? value) => FilterChanged();

    private void FilterChanged()
    {
        OnPropertyChanged(nameof(IsLineChart));
        OnPropertyChanged(nameof(IsPieChart));
        OnPropertyChanged(nameof(IsProjectData));
        OnPropertyChanged(nameof(ShowPatternTypeFilter));
        OnPropertyChanged(nameof(ShowProjectGroupingFilter));

        if (!_isInitialized)
            return;

        _ = LoadAsync();
    }
}
