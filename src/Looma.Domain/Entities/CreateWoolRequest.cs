namespace Looma.Domain.Entities;

public sealed record CreateWoolRequest(
    string Name,
    string Brand,
    string Material,
    string Color,
    double LengthToWeightRatio,
    double NeedleMinSize,
    double NeedleMaxSize);