// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.ComponentModel.DataAnnotations;

namespace Looma.Domain.Core;

public enum StockAdjustmentMode
{
    [Display(Name = "Par pelote")]
    ByBall,
    [Display(Name = "Par poids (g)")]
    ByWeight,
    [Display(Name = "Par longueur (m)")]
    ByLength
}