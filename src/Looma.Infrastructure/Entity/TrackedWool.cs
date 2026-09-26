// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Infrastructure.Entity;

public class TrackedWool
{
    public string Id { get; set; } = null!;
    public DateTime Date { get; set; }
    public double Quantity { get; set; }

    // Null lorsque la laine a été supprimée : le mouvement est conservé avec ses valeurs figées.
    public int? WoolId { get; set; }
    public WoolEntity? WoolEntity { get; set; }

    public int? ProjectId { get; set; }
    public ProjectEntity? ProjectEntity { get; set; }

    // Instantané de la laine et du projet au moment du mouvement (null pour les lignes écrites
    // par une ancienne version de l'application : on retombe alors sur les données actuelles).
    public string? WoolName { get; set; }
    public string? WoolBrand { get; set; }
    public string? WoolMaterial { get; set; }
    public string? WoolColor { get; set; }
    public double? WoolWeight { get; set; }
    public double? WoolLength { get; set; }
    public double? WoolNeedleMinSize { get; set; }
    public double? WoolNeedleMaxSize { get; set; }
    public string? ProjectName { get; set; }
    public PatternType? PatternType { get; set; }
}
