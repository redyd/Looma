namespace Looma.Domain.Entities;

public sealed record CreateDocumentRequest(
    string SourcePath,
    string? Nickname,
    int? PatternId = null);
