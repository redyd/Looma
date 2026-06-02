namespace Looma.Domain.Entities;

public sealed record UpdateWoolRequest(
    int Id,
    string? Name,
    string? Brand,
    string? Material,
    string? Color,
    double? LengthToWeightRatio,
    double? NeedleMinSize,
    double? NeedleMaxSize);
