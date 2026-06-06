namespace Looma.Domain.Entities;

public class Wool
{
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
}