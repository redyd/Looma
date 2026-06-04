namespace Looma.Domain.Entities;

public sealed record UpdatePatternRequest(
    int Id,
    string Name,
    string? Url,
    string? Note);
