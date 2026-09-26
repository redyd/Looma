// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public sealed record TrackedWoolMovement(
    string Id,
    DateTime Date,
    double Quantity,
    int? WoolId,
    string WoolName,
    string WoolBrand,
    int? ProjectId,
    string? ProjectName,
    Status? ProjectStatus,
    PatternType? PatternType)
{
    public string WoolMaterial { get; init; } = string.Empty;
    public IReadOnlyList<string> WoolColors { get; init; } = [];

    /// <summary>Poids d'une pelote (g) au moment du mouvement.</summary>
    public double WoolWeight { get; init; }

    /// <summary>Longueur d'une pelote (m) au moment du mouvement.</summary>
    public double WoolLength { get; init; }

    public double WoolNeedleMinSize { get; init; }
    public double WoolNeedleMaxSize { get; init; }

    /// <summary>False lorsque la laine a été supprimée depuis : seules les valeurs figées restent.</summary>
    public bool WoolExists => WoolId is not null;

    /// <summary>Quantité en pelotes (le stock est stocké en millièmes de pelote).</summary>
    public double Skeins => Quantity / 1000;
}
