// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Presentation.Services;

namespace Looma.Presentation.ViewModels.Shared.Projects;

public partial class ProjectWoolUsageViewModel(WoolUsage usage, ICommand addCommand) : ObservableObject
{
    public WoolUsage Usage { get; } = usage;
    public ICommand AddCommand { get; } = addCommand;
    [ObservableProperty]
    public partial StockAdjustmentMode DisplayMode { get; set; } = StockAdjustmentMode.ByBall;

    public string Name => Usage.Wool.Name;
    public string Brand => Usage.Wool.Brand;
    public List<string> Colors => Usage.Wool.Colors;
    public string AvailableDisplay => FormatStock(Usage.RemainingStock);
    public string UsedDisplay => FormatStock(Usage.StockUsed);
    public string AlreadyDeductedDisplay => FormatStock(Usage.StockAlreadyUsed);
    public string AvailableSummary => TranslationService.Current.Format("ProjectsDetail_WoolInStock", AvailableDisplay);
    public string UsedSummary => TranslationService.Current.Format("ProjectsDetail_WoolUsed", UsedDisplay);
    public string AlreadyDeductedSummary => TranslationService.Current.Format("ProjectsDetail_WoolAlreadyDeducted", AlreadyDeductedDisplay);

    partial void OnDisplayModeChanged(StockAdjustmentMode value)
    {
        RefreshTranslations();
    }

    public void RefreshTranslations()
    {
        OnPropertyChanged(nameof(AvailableDisplay));
        OnPropertyChanged(nameof(UsedDisplay));
        OnPropertyChanged(nameof(AlreadyDeductedDisplay));
        OnPropertyChanged(nameof(AvailableSummary));
        OnPropertyChanged(nameof(UsedSummary));
        OnPropertyChanged(nameof(AlreadyDeductedSummary));
    }

    private string FormatStock(double stock) =>
        DisplayMode switch
        {
            StockAdjustmentMode.ByWeight => $"{stock / 1000 * Usage.Wool.Weight:N0} g",
            StockAdjustmentMode.ByLength => $"{stock / 1000 * Usage.Wool.Length:N0} m",
            _ => $"{Math.Max(0, stock / 1000):N2} {TranslationService.Current["Common_SkeinsUnit"]}"
        };
}
