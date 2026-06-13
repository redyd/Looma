// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public class ProjectSelectablePatternViewModel(Pattern pattern, bool isSelected, ICommand selectCommand)
{
    public Pattern Pattern { get; } = pattern;
    public bool IsSelected { get; } = isSelected;
    public ICommand SelectCommand { get; } = selectCommand;
    public string TypeDisplay => Pattern.Type.GetDisplayName();
    public string DetailDisplay => $"{TypeDisplay} - {(Pattern.IsPersonal ? "Personnel" : "Non personnel")}";
    public string OriginDisplay => Pattern.IsPersonal ? "Personnel" : "Non personnel";
    public string BeginDateDisplay => FormatDate(Pattern.BeginDate);
    public string EndDateDisplay => FormatDate(Pattern.EndDate);
    public string SelectionDisplay => IsSelected ? "Sélectionné" : "Choisir";

    private static string FormatDate(DateOnly? value) =>
        value is null ? "Aucune" : value.Value.ToString("dd/MM/yyyy");
}
