namespace Looma.Domain.Entities;

public sealed record CreatePatternRequest(
    string Name,
    string? Url,
    string? Note,
    IReadOnlyList<Guid> DocumentIds);
