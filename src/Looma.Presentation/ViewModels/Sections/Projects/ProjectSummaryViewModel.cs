// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public class ProjectSummaryViewModel(Project project, ICommand openDetailCommand)
{
    public Project Project { get; } = project;
    public ICommand OpenDetailCommand { get; } = openDetailCommand;
    public string StatusDisplay => Project.Status.GetDisplayName();
    public string PatternName => Project.Pattern?.Name ?? "Aucun patron";
    public string BeginDateDisplay => FormatDate(Project.BeginDate);
    public string EndDateDisplay => FormatDate(Project.EndDate);
    public string WoolCountDisplay => $"{Project.Wools.Count:N0} laine(s)";
    public string NotePreview => string.IsNullOrWhiteSpace(Project.Note) ? "Aucune note." : Project.Note!;

    private static string FormatDate(DateOnly? value) =>
        value is null ? "Aucune" : value.Value.ToString("dd/MM/yyyy");
}
