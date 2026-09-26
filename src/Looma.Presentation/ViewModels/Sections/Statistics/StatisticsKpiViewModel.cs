// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Presentation.ViewModels.Sections.Statistics;

public enum StatisticsTrend
{
    None,
    Up,
    Down,
    Stable
}

public sealed record StatisticsKpiViewModel(
    string Title,
    string Value,
    string? Caption,
    string Icon,
    StatisticsTrend Trend = StatisticsTrend.None,
    string? TrendText = null)
{
    public bool HasCaption => !string.IsNullOrWhiteSpace(Caption);
    public bool HasTrend => Trend != StatisticsTrend.None && !string.IsNullOrWhiteSpace(TrendText);
    public bool IsTrendUp => Trend == StatisticsTrend.Up;
    public bool IsTrendDown => Trend == StatisticsTrend.Down;
    public bool IsTrendStable => Trend == StatisticsTrend.Stable;
}
