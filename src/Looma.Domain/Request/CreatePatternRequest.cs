using Looma.Domain.Core;

namespace Looma.Domain.Request;

public sealed record CreatePatternRequest(
    string Name,
    string? Url,
    string? Note,
    PatternType Type,
    bool IsPersonal,
    DateOnly? BeginDate = null,
    DateOnly? EndDate = null);
