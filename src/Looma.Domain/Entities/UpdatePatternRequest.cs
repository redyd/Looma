using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public sealed record UpdatePatternRequest(
    int Id,
    string Name,
    string? Url,
    string? Note,
    PatternType Type,
    bool IsPersonal,
    DateOnly? BeginDate = null,
    DateOnly? EndDate = null);
