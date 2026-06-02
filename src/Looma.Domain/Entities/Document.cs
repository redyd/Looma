namespace Looma.Domain.Entities;

public class Document
{
    public required Guid Id { get; init; }
    public required string Nickname { get; init; }
    public required string Type { get; init; }
    public required long SizeBytes { get; init; }
}
