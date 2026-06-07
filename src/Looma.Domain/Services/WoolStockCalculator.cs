// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Services;

public class WoolStockCalculator
{
    public double ComputeStockQuantity(StockAdjustmentMode mode, bool isAddition, double quantity, double factor)
    {
        var data = mode switch
        {
            StockAdjustmentMode.ByBall => quantity * 1000,
            StockAdjustmentMode.ByWeight or StockAdjustmentMode.ByLength => quantity / factor * 1000,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return isAddition ? data : -data;
    }
}