// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Statistics;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Sections.Statistics;

/// <summary>Onglet « Global » prêt à afficher : projets, patrons et bibliothèque.</summary>
public sealed class StatisticsGlobalReportViewModel
{
    public static StatisticsGlobalReportViewModel Empty { get; } = new();

    private StatisticsGlobalReportViewModel()
    {
    }

    public IReadOnlyList<StatisticsKpiViewModel> Kpis { get; private init; } = [];
    public IReadOnlyList<StatisticsKpiViewModel> Library { get; private init; } = [];
    public StatisticsTimelineChart Timeline { get; private init; } = StatisticsTimelineChart.Empty;
    public StatisticsDonutChart ByStatus { get; private init; } = StatisticsDonutChart.Empty;
    public StatisticsDonutChart ByPatternType { get; private init; } = StatisticsDonutChart.Empty;
    public IReadOnlyList<StatisticsRankRowViewModel> LongestProjects { get; private init; } = [];

    public bool HasData { get; private init; }
    public bool HasLongestProjects => LongestProjects.Count > 0;

    public static StatisticsGlobalReportViewModel From(GlobalStatistics stats, TranslationService translation)
    {
        string Count(double value) => StatisticsFormatter.Count(value);
        var (trend, trendText) = StatisticsFormatter.Trend(stats.FinishedInPeriod, stats.FinishedPreviousPeriod, translation);
        var maxDays = stats.LongestProjects.Select(p => p.Days).DefaultIfEmpty(0).Max();

        return new StatisticsGlobalReportViewModel
        {
            HasData = !stats.IsEmpty,
            Kpis =
            [
                new(translation["Statistics_Kpi_FinishedInPeriod"], Count(stats.FinishedInPeriod),
                    translation.Format("Statistics_Kpi_FinishedTotalCaption", stats.ProjectsFinished),
                    "CircleCheckBig", trend, trendText),
                new(translation["Statistics_Kpi_StartedInPeriod"], Count(stats.StartedInPeriod),
                    translation.Format("Statistics_Kpi_InProgressCaption", stats.ProjectsInProgress), "Hourglass"),
                new(translation["Statistics_Kpi_AverageDuration"],
                    stats.AverageDurationDays is { } days ? StatisticsFormatter.Days(days, translation) : "—",
                    translation["Statistics_Kpi_AverageDurationCaption"], "Timer"),
                new(translation["Statistics_Kpi_Projects"], Count(stats.TotalProjects),
                    translation["Statistics_Kpi_ProjectsCaption"], "FolderKanban")
            ],
            Library =
            [
                new(translation["Statistics_Kpi_Patterns"], Count(stats.TotalPatterns),
                    translation.Format("Statistics_Kpi_PersonalPatternsCaption", stats.PersonalPatterns), "BookOpen"),
                new(translation["Statistics_Kpi_Wools"], Count(stats.TotalWools), null, "Palette"),
                new(translation["Statistics_Kpi_Documents"], Count(stats.TotalDocuments), null, "Files")
            ],
            Timeline = StatisticsFormatter.Timeline(
                stats.Granularity,
                [
                    (translation["Statistics_Series_Started"], stats.StartedTimeline, StatisticsFormatter.SeriesBrush(1)),
                    (translation["Statistics_Series_Finished"], stats.FinishedTimeline, StatisticsFormatter.SeriesBrush(2))
                ],
                Count,
                isCount: true),
            ByStatus = StatisticsFormatter.Donut(
                stats.ProjectsByStatus,
                status => translation[$"Enum_{status}"],
                (status, _) => $"Status{status}RibbonBackgroundBrush",
                Count,
                translation),
            ByPatternType = StatisticsFormatter.Donut(
                stats.ProjectsByPatternType,
                type => type is null ? translation["Statistics_NoPattern"] : translation[$"Enum_{type}"],
                (type, _) => type is null ? "DividerBrush" : $"Pattern{type}RibbonBackgroundBrush",
                Count,
                translation),
            LongestProjects = stats.LongestProjects
                .Select((project, index) => new StatisticsRankRowViewModel(
                    index + 1,
                    project.Name,
                    translation[$"Enum_{project.Status}"],
                    StatisticsFormatter.Days(project.Days, translation),
                    maxDays > 0 ? (double)project.Days / maxDays : 0,
                    []))
                .ToList()
        };
    }
}
