// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using CommunityToolkit.Mvvm.ComponentModel;
using Looma.Domain.Core;
using Looma.Domain.Entities;

namespace Looma.Presentation.ViewModels.Sections.Projects;

public partial class ProjectFinishWoolViewModel(
    WoolUsage usage,
    StockAdjustmentMode deductionMode,
    Action quantityChanged) : ObservableObject
{
    private StockAdjustmentMode _deductionMode = deductionMode;

    public WoolUsage Usage { get; } = usage;
    public string Name => Usage.Wool.Name;
    public string Brand => Usage.Wool.Brand;
    public List<string> Colors => Usage.Wool.Colors;
    public string AvailableDisplay => $"{Usage.Wool.Stock / 1000:N2} pelote(s)";
    public string AlreadyDeductedDisplay => $"{Usage.StockAlreadyUsed / 1000:N2} pelote(s)";

    public double QuantityToDeduct
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(StockToDeduct));
            quantityChanged();
        }
    } = FormatStockQuantity(usage.PendingStockToDeduct, usage.Wool, deductionMode);

    public double StockToDeduct => ComputeStockQuantity(QuantityToDeduct);

    public void SetDeductionMode(StockAdjustmentMode value)
    {
        if (_deductionMode == value)
            return;

        var stockToDeduct = StockToDeduct;
        _deductionMode = value;
        QuantityToDeduct = FormatStockQuantity(stockToDeduct);
    }

    public double FormatStockQuantity(double stock) =>
        FormatStockQuantity(stock, Usage.Wool, _deductionMode);

    private double ComputeStockQuantity(double quantity) =>
        _deductionMode switch
        {
            StockAdjustmentMode.ByWeight => quantity / Usage.Wool.Weight * 1000,
            StockAdjustmentMode.ByLength => quantity / Usage.Wool.Length * 1000,
            _ => quantity * 1000
        };

    private static double FormatStockQuantity(double stock, Wool wool, StockAdjustmentMode mode) =>
        mode switch
        {
            StockAdjustmentMode.ByWeight => stock / 1000 * wool.Weight,
            StockAdjustmentMode.ByLength => stock / 1000 * wool.Length,
            _ => stock / 1000
        };
}