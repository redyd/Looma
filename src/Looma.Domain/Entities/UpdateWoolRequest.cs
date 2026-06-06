namespace Looma.Domain.Entities;

public sealed record UpdateWoolRequest(
    int Id,
    string Name,
    string Brand,
    string Material,
    string Color,
    double Weight,
    double Length,
    double Stock,
    double NeedleMinSize,
    double NeedleMaxSize);