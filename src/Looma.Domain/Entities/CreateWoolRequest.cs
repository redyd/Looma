namespace Looma.Domain.Entities;

public sealed record CreateWoolRequest(
    string Name,
    string Brand,
    string Material,
    string Color,
    double Weight,
    double Length,
    double Stock,
    double NeedleMinSize,
    double NeedleMaxSize);