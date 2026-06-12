// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Patterns;

public record PatternProjectViewModel(PatternProject Project)
{
    public string StatusDisplay => Project.Status switch
    {
        Status.Wishlist => "Wishlist",
        Status.InProgress => "En cours",
        Status.Finished => "Terminé",
        Status.Paused => "En pause",
        _ => Project.Status.ToString()
    };
}
