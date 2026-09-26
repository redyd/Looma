// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Statistics;

/// <summary>Consommation et stock de laine sur une période, dans l'unité demandée.</summary>
public sealed record WoolStatistics
{
    public required StatisticsGranularity Granularity { get; init; }
    public required StatisticsQuantityUnit Unit { get; init; }

    public required double Used { get; init; }
    public required double Added { get; init; }

    /// <summary>Consommation sur la période précédente de même durée, null pour « tout ».</summary>
    public required double? UsedPreviousPeriod { get; init; }

    public required double CurrentStock { get; init; }
    public required int WoolsInStock { get; init; }
    public required int ProjectsSupplied { get; init; }

    public required IReadOnlyList<StatisticsBucketValue> UsedTimeline { get; init; }
    public required IReadOnlyList<StatisticsBucketValue> AddedTimeline { get; init; }

    public required IReadOnlyList<StatisticsShare<string>> UsedByMaterial { get; init; }
    public required IReadOnlyList<StatisticsShare<WoolType>> UsedByWeightClass { get; init; }
    public required IReadOnlyList<StatisticsShare<WoolType>> StockByWeightClass { get; init; }

    public required IReadOnlyList<StatisticsWoolRanking> TopWools { get; init; }
    public required IReadOnlyList<StatisticsProjectConsumption> TopProjects { get; init; }

    public bool HasMovements => Used > 0 || Added > 0;

    /// <summary>Évolution en pourcentage par rapport à la période précédente, null si non comparable.</summary>
    public double? UsedTrendPercent =>
        UsedPreviousPeriod is > 0 ? (Used - UsedPreviousPeriod.Value) / UsedPreviousPeriod.Value * 100 : null;
}
