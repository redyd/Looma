// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;

namespace Looma.Presentation.ViewModels.Shared.Patterns;

public record PatternSummaryViewModel(
    Pattern Pattern,
    int DocumentCount,
    int ProjectCount,
    bool HasUrl,
    ICommand OpenDetailCommand)
{
    public bool HasBeginDate => Pattern.BeginDate is not null;
    public string BeginDateDisplay => Pattern.BeginDate.FormatWithDefault("Aucune");
    public bool HasEndDate => Pattern.EndDate is not null;
    public string EndDateDisplay => Pattern.EndDate.FormatWithDefault("Aucune");
    public string TypeDisplay => Pattern.Type.GetDisplayName();
}
