// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Statistics;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Sections.Statistics;

/// <summary>Onglet « Laine » prêt à afficher : chiffres clés, graphiques et classements.</summary>
public sealed class StatisticsWoolReportViewModel
{
    public static StatisticsWoolReportViewModel Empty { get; } = new();

    private StatisticsWoolReportViewModel()
    {
    }

    public IReadOnlyList<StatisticsKpiViewModel> Kpis { get; private init; } = [];
    public StatisticsTimelineChart Timeline { get; private init; } = StatisticsTimelineChart.Empty;
    public StatisticsDonutChart ByMaterial { get; private init; } = StatisticsDonutChart.Empty;
    public StatisticsDonutChart ByWeightClass { get; private init; } = StatisticsDonutChart.Empty;
    public StatisticsDonutChart StockByWeightClass { get; private init; } = StatisticsDonutChart.Empty;
    public IReadOnlyList<StatisticsRankRowViewModel> TopWools { get; private init; } = [];
    public IReadOnlyList<StatisticsRankRowViewModel> TopProjects { get; private init; } = [];

    public bool HasData { get; private init; }
    public bool HasTopWools => TopWools.Count > 0;
    public bool HasTopProjects => TopProjects.Count > 0;

    public static StatisticsWoolReportViewModel From(WoolStatistics stats, TranslationService translation)
    {
        string Quantity(double value) => StatisticsFormatter.Quantity(value, stats.Unit, translation);
        var (trend, trendText) = StatisticsFormatter.Trend(stats.Used, stats.UsedPreviousPeriod, translation);
        var maxWool = stats.TopWools.Select(w => w.Used).DefaultIfEmpty(0).Max();
        var maxProject = stats.TopProjects.Select(p => p.Used).DefaultIfEmpty(0).Max();

        return new StatisticsWoolReportViewModel
        {
            HasData = stats.HasMovements || stats.WoolsInStock > 0,
            Kpis =
            [
                new(translation["Statistics_Kpi_Used"], Quantity(stats.Used),
                    translation["Statistics_Kpi_UsedCaption"], "Layers", trend, trendText),
                new(translation["Statistics_Kpi_Added"], Quantity(stats.Added),
                    translation["Statistics_Kpi_AddedCaption"], "ShoppingBag"),
                new(translation["Statistics_Kpi_Stock"], Quantity(stats.CurrentStock),
                    translation.Format("Statistics_Kpi_StockCaption", stats.WoolsInStock), "Boxes"),
                new(translation["Statistics_Kpi_ProjectsSupplied"], StatisticsFormatter.Count(stats.ProjectsSupplied),
                    translation["Statistics_Kpi_ProjectsSuppliedCaption"], "FolderKanban")
            ],
            Timeline = StatisticsFormatter.Timeline(
                stats.Granularity,
                [
                    (translation["Statistics_Series_Used"], stats.UsedTimeline, StatisticsFormatter.SeriesBrush(0)),
                    (translation["Statistics_Series_Added"], stats.AddedTimeline, StatisticsFormatter.SeriesBrush(2))
                ],
                Quantity),
            ByMaterial = StatisticsFormatter.Donut(
                stats.UsedByMaterial,
                material => material.Length == 0 ? translation["Statistics_Unknown"] : material,
                (_, index) => StatisticsFormatter.SeriesBrush(index),
                Quantity,
                translation),
            ByWeightClass = StatisticsFormatter.Donut(
                stats.UsedByWeightClass,
                type => translation[$"Enum_{type}"],
                (type, _) => StatisticsFormatter.SeriesBrush((int)type),
                Quantity,
                translation),
            StockByWeightClass = StatisticsFormatter.Donut(
                stats.StockByWeightClass,
                type => translation[$"Enum_{type}"],
                (type, _) => StatisticsFormatter.SeriesBrush((int)type),
                Quantity,
                translation),
            TopWools = stats.TopWools
                .Select((wool, index) => new StatisticsRankRowViewModel(
                    index + 1,
                    wool.Name,
                    wool.Brand,
                    Quantity(wool.Used),
                    maxWool > 0 ? wool.Used / maxWool : 0,
                    wool.Colors,
                    wool.Exists ? null : translation["Statistics_Badge_Deleted"]))
                .ToList(),
            TopProjects = stats.TopProjects
                .Select((project, index) => new StatisticsRankRowViewModel(
                    index + 1,
                    project.Name.Length == 0 ? translation["Common_NoName"] : project.Name,
                    translation.Format("Statistics_Rank_WoolCount", project.WoolCount),
                    Quantity(project.Used),
                    maxProject > 0 ? project.Used / maxProject : 0,
                    [],
                    project.ProjectId is null ? translation["Statistics_Badge_Deleted"] : null))
                .ToList()
        };
    }
}
