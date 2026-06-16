// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Logging;

namespace Looma.Domain.Services;

public class WoolStockCalculator(IDomainLogger? logger = null)
{
    private readonly IDomainLogger _logger = logger ?? NullDomainLogger.Instance;

    public double ComputeStockQuantity(StockAdjustmentMode mode, bool isAddition, double quantity, double factor)
    {
        _logger.Log(DomainLogLevel.Information, $"WoolStockCalculator.ComputeStockQuantity({mode}, addition:{isAddition}) started.");

        var data = mode switch
        {
            StockAdjustmentMode.ByBall => quantity * 1000,
            StockAdjustmentMode.ByWeight or StockAdjustmentMode.ByLength => quantity / factor * 1000,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        var result = isAddition ? data : -data;
        _logger.Log(DomainLogLevel.Information, $"WoolStockCalculator.ComputeStockQuantity({mode}, addition:{isAddition}) completed.");
        return result;
    }
}
