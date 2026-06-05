using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public class Pattern
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string? Url { get; init; }
    public required string? Note { get; init; }
    public required DateOnly? BeginDate { get; init; }
    public required DateOnly? EndDate { get; init; }
    public required PatternType Type { get; init; }
    public required bool IsPersonal { get; init; }
    public required IReadOnlyList<Document> Documents { get; init; }
    public required IReadOnlyList<PatternProject> Projects { get; init; }
}
