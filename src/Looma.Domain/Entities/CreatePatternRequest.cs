namespace Looma.Domain.Entities;

public sealed record CreatePatternRequest(
    string Name,
    string? Url,
    string? Note,
    DateOnly? BeginDate = null,
    DateOnly? EndDate = null);
