namespace Looma.Domain.Entities;

public class Wool
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Brand { get; init; }
    public required string Material { get; init; }
    public required string Color { get; init; }
    public required double LengthToWeightRatio { get; init; }
    public required double NeedleMinSize { get; init; }
    public required double NeedleMaxSize { get; init; }
}