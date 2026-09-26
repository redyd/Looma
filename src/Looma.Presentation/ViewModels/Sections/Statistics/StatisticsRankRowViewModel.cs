// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Presentation.ViewModels.Sections.Statistics;

/// <summary>Ligne d'un classement : <see cref="Ratio"/> (0..1) dimensionne la barre de proportion.</summary>
public sealed record StatisticsRankRowViewModel(
    int Rank,
    string Title,
    string Subtitle,
    string Value,
    double Ratio,
    IReadOnlyList<string> Colors,
    string? Badge = null)
{
    public bool HasColors => Colors.Count > 0;
    public bool HasBadge => !string.IsNullOrWhiteSpace(Badge);
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
}
