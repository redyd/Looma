// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Statistics;

public sealed record StatisticsSnapshot(
    IReadOnlyList<string> Labels,
    IReadOnlyList<StatisticsSeries> Series,
    IReadOnlyList<StatisticsSlice> Slices)
{
    public bool IsEmpty =>
        Series.All(series => series.Points.All(point => point.Value == 0))
        && Slices.Count == 0;
}
