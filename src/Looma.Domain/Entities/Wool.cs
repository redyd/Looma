using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public class Wool
{
    private static readonly (WoolType Type, double Min, double Max)[] WoolRanges =
    [
        (WoolType.Lace, 1.5, 2.25),
        (WoolType.SuperFine, 2.25, 3.25),
        (WoolType.Fine, 3.25, 3.75),
        (WoolType.Light, 3.75, 4.5),
        (WoolType.Medium, 4.5, 5.5),
        (WoolType.Bulky, 5.5, 8),
        (WoolType.SuperBulky, 8, 12.75),
        (WoolType.Jumbo, 12.75, double.MaxValue)
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