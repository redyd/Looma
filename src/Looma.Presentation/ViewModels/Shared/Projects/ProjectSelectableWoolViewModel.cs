// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Shared.Projects;

public class ProjectSelectableWoolViewModel(Wool wool, bool isSelected, ICommand toggleCommand)
{
    public Wool Wool { get; } = wool;
    public bool IsSelected { get; } = isSelected;
    public ICommand ToggleCommand { get; } = toggleCommand;
    public string DetailDisplay => $"{Wool.Brand} - {Wool.Material}";
    public string StockDisplay => $"{Wool.BatchQuantity:N2} {TranslationService.Current["Common_SkeinsUnit"]}";
    public string SelectionDisplay => IsSelected ? TranslationService.Current["Common_SelectedFeminine"] : TranslationService.Current["Common_Add"];
}
