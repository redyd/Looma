// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;

namespace Looma.Presentation.ViewModels.Shared.Projects;

public class ProjectSummaryViewModel(Project project, ICommand openDetailCommand)
{
    public Project Project { get; } = project;
    public ICommand OpenDetailCommand { get; } = openDetailCommand;
    public string StatusDisplay => Project.Status.GetDisplayName();
    public bool HasPatternType => Project.Pattern is not null;
    public string? PatternTypeDisplay => Project.Pattern?.Type.GetDisplayName();
    public string PatternName => Project.Pattern?.Name ?? "Aucun patron";
    public bool HasBeginDate => Project.BeginDate is not null;
    public string BeginDateDisplay => Project.BeginDate.FormatWithDefault("Aucune");
    public bool HasEndDate => Project.EndDate is not null;
    public string EndDateDisplay => Project.EndDate.FormatWithDefault("Aucune");
    public string WoolCountDisplay => $"{Project.Wools.Count:N0} laine(s)";
}
