using Looma.Domain.Core;

namespace Looma.Domain.Entities;

public class Project
{
    public required int ProjectId { get; init; }
    public required string Name { get; init; }
    public required Status Status { get; init; }
    public required Pattern Pattern { get; init; }

    public required string? Note { get; init; }
    public required DateOnly? BeginDate { get; init; }
    public required DateOnly? EndDate { get; init; }

    public IReadOnlyList<WoolUsage> Wools { get; init; } = [];
    public IReadOnlyList<Document> Files { get; init; } = [];
}
