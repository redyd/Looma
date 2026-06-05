using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public class PatternProject
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required Status Status { get; init; }
}
