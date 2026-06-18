// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public class Wool
{
    public static readonly IReadOnlyList<WoolNeedleRange> NeedleRanges =
    [
        new(WoolType.Lace, 1, 2),
        new(WoolType.SuperFine, 2.25, 3),
        new(WoolType.Fine, 3.25, 3.75),
        new(WoolType.Light, 4, 4.75),
        new(WoolType.Medium, 5, 5.75),
        new(WoolType.Bulky, 6, 8.25),
        new(WoolType.SuperBulky, 8.5, 13.75),
        new(WoolType.Jumbo, 14, double.MaxValue)
    ];

    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Brand { get; init; }
    public required string Material { get; init; }
    public required List<string> Colors { get; init; }
    public required double Weight { get; init; }
    public required double Length { get; init; }
    public required double Stock { get; init; }
    public required double NeedleMinSize { get; init; }
    public required double NeedleMaxSize { get; init; }

    public double BatchQuantity => Stock / 1000;
    public double StockWeight => Weight * BatchQuantity;
    public double StockLength => Length * BatchQuantity;

    public List<WoolType> Types =>
        [.. NeedleRanges
            .Where(r => r.Contains(NeedleMinSize, NeedleMaxSize))
            .Select(r => r.Type)];

    public static WoolNeedleRange? FindNeedleRange(double needleMinSize, double needleMaxSize) =>
        NeedleRanges.FirstOrDefault(r => r.Matches(needleMinSize, needleMaxSize));

    public static WoolNeedleRange? FindContainingNeedleRange(double needleMinSize, double needleMaxSize) =>
        FindNeedleRange(needleMinSize, needleMaxSize)
        ?? NeedleRanges.FirstOrDefault(r => r.Contains(needleMinSize, needleMaxSize));
}
