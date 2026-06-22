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

    public override bool KeepAliveInNavigationHistory => true;

    public IReadOnlyList<StatisticsOptionViewModel<StatisticsRange>> RangeOptions { get; } =
    [
        new("Tout", StatisticsRange.All),
        new("Cette année", StatisticsRange.ThisYear),
        new("Ces 6 derniers mois", StatisticsRange.LastSixMonths),
        new("Ce mois", StatisticsRange.ThisMonth),
        new("Cette semaine", StatisticsRange.ThisWeek)
    ];

    public IReadOnlyList<StatisticsOptionViewModel<StatisticsQuantityUnit>> QuantityUnitOptions { get; } =
    [
        new("Pelotes", StatisticsQuantityUnit.Skein),
        new("Poids", StatisticsQuantityUnit.Weight),
        new("Longueur", StatisticsQuantityUnit.Length)
    ];

    public IReadOnlyList<StatisticsOptionViewModel<PatternType?>> PatternTypeOptions { get; } =
    [
        new("Tout", null),
        ..Enum.GetValues<PatternType>()
            .Select(type => new StatisticsOptionViewModel<PatternType?>(type.GetDisplayName(), type))
    ];

    [ObservableProperty]
    public partial StatisticsOptionViewModel<StatisticsRange>? SelectedRange { get; set; }

    [ObservableProperty]
    public partial StatisticsOptionViewModel<StatisticsQuantityUnit>? SelectedQuantityUnit { get; set; }

    [ObservableProperty]
    public partial StatisticsOptionViewModel<PatternType?>? SelectedPatternType { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> Labels { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<StatisticsSeries> Series { get; set; } = [];

    [ObservableProperty]
    public partial bool HasData { get; set; }

    public override async void OnNavigatedTo()
    {
        RegisterRefresh(refreshService, RefreshScope.Wools, LoadAsync);

        if (!_isInitialized)
        {
            Title = "Statistiques";
            SelectedRange = RangeOptions.First();
            SelectedQuantityUnit = QuantityUnitOptions.First();
            SelectedPatternType = PatternTypeOptions.First();
            _isInitialized = true;
        }

        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!_isInitialized || SelectedRange is null || SelectedQuantityUnit is null || SelectedPatternType is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var query = new StatisticsQuery(
                StatisticsChartKind.Line,
                StatisticsDataKind.Wool,
                SelectedRange.Value,
                SelectedPatternType.Value,
                null,
                StatisticsProjectGrouping.Status,
                SelectedQuantityUnit.Value,
                DateOnly.FromDateTime(DateTime.Today));

            var result = await statisticsService.GetAsync(query);
            if (result.Failed || result.Value is null)
            {
                notifications.Error(result.Error ?? "Impossible de charger les statistiques.");
                Labels = [];
                Series = [];
                HasData = false;
                return;
            }

            Labels = result.Value.Labels;
            Series = result.Value.Series;
            HasData = !result.Value.IsEmpty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedRangeChanged(StatisticsOptionViewModel<StatisticsRange>? value)
    {
        if (!_isInitialized)
            return;

        _ = LoadAsync();
    }

    partial void OnSelectedPatternTypeChanged(StatisticsOptionViewModel<PatternType?>? value)
    {
        if (!_isInitialized)
            return;

        _ = LoadAsync();
    }

    partial void OnSelectedQuantityUnitChanged(StatisticsOptionViewModel<StatisticsQuantityUnit>? value)
    {
        if (!_isInitialized)
            return;

        _ = LoadAsync();
    }
}
