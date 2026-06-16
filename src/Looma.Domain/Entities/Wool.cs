// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public class Wool
{
    private static readonly (WoolType Type, double Min, double Max)[] WoolRanges =
    [
        (WoolType.Lace, 1, 2),
        (WoolType.SuperFine, 2.25, 3),
        (WoolType.Fine, 3, 3.75),
        (WoolType.Light, 3.75, 4.75),
        (WoolType.Medium, 4.75, 5.75),
        (WoolType.Bulky, 5.75, 8.25),
        (WoolType.SuperBulky, 8.25, 13.75),
        (WoolType.Jumbo, 13.75, double.MaxValue)
    ];

    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Brand { get; init; }
    public required string Material { get; init; }
    public required string Color { get; init; }
    public required double Weight { get; init; }
    public required double Length { get; init; }
    public required double Stock { get; init; }
    public required double NeedleMinSize { get; init; }
    public required double NeedleMaxSize { get; init; }

    public double BatchQuantity => Stock / 1000;
    public string BatchQuantityDisplay => $"{BatchQuantity:F1} pelote{(BatchQuantity > 1 ? "s" : "")}";
    public double StockWeight => Weight * BatchQuantity;
    public double StockLength => Length * BatchQuantity;

    public List<WoolType> Types =>
        [.. WoolRanges
            .Where(r => NeedleMinSize <= r.Max && NeedleMaxSize >= r.Min)
            .Select(r => r.Type)];
}
