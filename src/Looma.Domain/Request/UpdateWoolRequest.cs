namespace Looma.Domain.Request;

public sealed record UpdateWoolRequest(
    int Id,
    string Name,
    string Brand,
    string Material,
    string Color,
    double Weight,
    double Length,
    double NeedleMinSize,
    double NeedleMaxSize);