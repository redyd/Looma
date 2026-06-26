// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Looma.Domain.Core;
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
    private bool _isRefreshingOptions;
    private bool _isListeningTranslation;

    public override bool KeepAliveInNavigationHistory => true;

    public ObservableCollection<StatisticsOptionViewModel<StatisticsRange>> RangeOptions { get; } = [];

    public ObservableCollection<StatisticsOptionViewModel<StatisticsQuantityUnit>> QuantityUnitOptions { get; } = [];

    public ObservableCollection<StatisticsOptionViewModel<PatternType?>> PatternTypeOptions { get; } = [];

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
        RegisterTranslationRefresh();
        RefreshOptions();

        if (!_isInitialized)
        {
            SelectedRange = RangeOptions.First();
            SelectedQuantityUnit = QuantityUnitOptions.First();
            SelectedPatternType = PatternTypeOptions.First();
            _isInitialized = true;
        }

        await LoadAsync();
    }

    protected override void OnDestroy()
    {
        if (_isListeningTranslation)
        {
            Translation.PropertyChanged -= OnTranslationChanged;
            _isListeningTranslation = false;
        }

        base.OnDestroy();
    }

    private void RegisterTranslationRefresh()
    {
        if (_isListeningTranslation)
            return;

        Translation.PropertyChanged += OnTranslationChanged;
        _isListeningTranslation = true;
    }

    private void OnTranslationChanged(object? sender, PropertyChangedEventArgs e) => RefreshOptions();

    private void RefreshOptions()
    {
        var previousRange = SelectedRange?.Value;
        var previousQuantityUnit = SelectedQuantityUnit?.Value;
        var previousPatternType = SelectedPatternType?.Value;

        _isRefreshingOptions = true;
        try
        {
            Title = Translation["Statistics_Title"];

            RangeOptions.Clear();
            RangeOptions.Add(new(Translation["Statistics_RangeAll"], StatisticsRange.All));
            RangeOptions.Add(new(Translation["Statistics_RangeThisYear"], StatisticsRange.ThisYear));
            RangeOptions.Add(new(Translation["Statistics_RangeLastSixMonths"], StatisticsRange.LastSixMonths));
            RangeOptions.Add(new(Translation["Statistics_RangeThisMonth"], StatisticsRange.ThisMonth));
            RangeOptions.Add(new(Translation["Statistics_RangeThisWeek"], StatisticsRange.ThisWeek));

            QuantityUnitOptions.Clear();
            QuantityUnitOptions.Add(new(Translation["Common_Skeins"], StatisticsQuantityUnit.Skein));
            QuantityUnitOptions.Add(new(Translation["Common_Weight"], StatisticsQuantityUnit.Weight));
            QuantityUnitOptions.Add(new(Translation["Common_Length"], StatisticsQuantityUnit.Length));

            PatternTypeOptions.Clear();
            PatternTypeOptions.Add(new(Translation["Statistics_RangeAll"], null));
            foreach (var type in Enum.GetValues<PatternType>())
                PatternTypeOptions.Add(new(Translation[$"Enum_{type}"], type));

            SelectedRange = RangeOptions.FirstOrDefault(option => option.Value == previousRange)
                            ?? RangeOptions.FirstOrDefault();
            SelectedQuantityUnit = QuantityUnitOptions.FirstOrDefault(option => option.Value == previousQuantityUnit)
                                   ?? QuantityUnitOptions.FirstOrDefault();
            SelectedPatternType = PatternTypeOptions.FirstOrDefault(option => option.Value == previousPatternType)
                                  ?? PatternTypeOptions.FirstOrDefault();
        }
        finally
        {
            _isRefreshingOptions = false;
        }
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
                notifications.Error(result.Error ?? Translation["Statistics_Notifications_UnableToLoadStatistics"]);
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
        if (!_isInitialized || _isRefreshingOptions)
            return;

        _ = LoadAsync();
    }

    partial void OnSelectedPatternTypeChanged(StatisticsOptionViewModel<PatternType?>? value)
    {
        if (!_isInitialized || _isRefreshingOptions)
            return;

        _ = LoadAsync();
    }

    partial void OnSelectedQuantityUnitChanged(StatisticsOptionViewModel<StatisticsQuantityUnit>? value)
    {
        if (!_isInitialized || _isRefreshingOptions)
            return;

        _ = LoadAsync();
    }
}
