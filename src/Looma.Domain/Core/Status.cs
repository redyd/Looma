// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.ComponentModel.DataAnnotations;

namespace Looma.Domain.Core;

public enum Status
{
    [Display(Name = "Wishlist")]
    Wishlist = 0,
    [Display(Name = "En cours")]
    InProgress = 1,
    [Display(Name = "Terminé")]
    Finished = 2,
    [Display(Name = "En pause")]
    Paused = 3
}
