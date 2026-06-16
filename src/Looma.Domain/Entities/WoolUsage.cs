// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

namespace Looma.Domain.Entities;

public class WoolUsage
{
    public required Wool Wool { get; init; }
    public required double StockUsed { get; init; }
    public required double StockAlreadyUsed { get; init; }

    public double RemainingStock => Wool.Stock;
    public double PendingStockToDeduct => Math.Max(0, StockUsed - StockAlreadyUsed);
}
