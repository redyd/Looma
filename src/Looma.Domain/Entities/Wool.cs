// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public class Wool
{
    private static readonly (WoolType Type, double Min, double Max)[] WoolRanges =
    [
        (WoolType.Lace, 1.5, 2.25),
        (WoolType.SuperFine, 2.25, 3.25),
        (WoolType.Fine, 3.25, 4),
        (WoolType.Light, 4, 5),
        (WoolType.Medium, 5, 6),
        (WoolType.Bulky, 6, 8.5),
        (WoolType.SuperBulky, 8.5, 14),
        (WoolType.Jumbo, 14, double.MaxValue)
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
    public double StockWeight => Weight * BatchQuantity;
    public double StockLength => Length * BatchQuantity;
    public List<WoolType> Types =>
        WoolRanges
            .Where(r => NeedleMinSize <= r.Max && NeedleMaxSize >= r.Min)
            .Select(r => r.Type)
            .ToList();
}