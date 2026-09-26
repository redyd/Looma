// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Statistics;

/// <summary>Vue d'ensemble des projets, patrons, laines et documents.</summary>
public sealed record GlobalStatistics
{
    public required StatisticsGranularity Granularity { get; init; }

    public required int TotalProjects { get; init; }
    public required int ProjectsInProgress { get; init; }
    public required int ProjectsFinished { get; init; }
    public required int FinishedInPeriod { get; init; }

    /// <summary>Projets terminés sur la période précédente de même durée, null pour « tout ».</summary>
    public required int? FinishedPreviousPeriod { get; init; }

    public required int StartedInPeriod { get; init; }
    public required double? AverageDurationDays { get; init; }

    public required int TotalPatterns { get; init; }
    public required int PersonalPatterns { get; init; }
    public required int TotalWools { get; init; }
    public required int TotalDocuments { get; init; }

    public required IReadOnlyList<StatisticsBucketValue> StartedTimeline { get; init; }
    public required IReadOnlyList<StatisticsBucketValue> FinishedTimeline { get; init; }

    public required IReadOnlyList<StatisticsShare<Status>> ProjectsByStatus { get; init; }

    /// <summary>Clé null : projets sans patron.</summary>
    public required IReadOnlyList<StatisticsShare<PatternType?>> ProjectsByPatternType { get; init; }

    public required IReadOnlyList<StatisticsProjectDuration> LongestProjects { get; init; }

    public bool IsEmpty => TotalProjects == 0 && TotalPatterns == 0 && TotalWools == 0 && TotalDocuments == 0;
}
