// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public class ProjectWoolUsageViewModel(WoolUsage usage, ICommand addCommand, ICommand removeCommand)
{
    public WoolUsage Usage { get; } = usage;
    public ICommand AddCommand { get; } = addCommand;
    public ICommand RemoveCommand { get; } = removeCommand;
    public string Name => Usage.Wool.Name;
    public string Brand => Usage.Wool.Brand;
    public string Color => Usage.Wool.Color;
    public string AvailableDisplay => $"{Math.Max(0, Usage.RemainingStock / 1000):N2} pelote(s)";
    public string UsedDisplay => $"{Usage.StockUsed / 1000:N2} pelote(s)";
}
