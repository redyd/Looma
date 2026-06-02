using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public class Pattern
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Url { get; init; }
    public string? Note { get; init; }
    public required IReadOnlyList<Document> Documents { get; init; }
    public required IReadOnlyList<PatternProject> Projects { get; init; }
}
