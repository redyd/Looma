namespace Looma.Domain.Request;

public sealed record CreateDocumentRequest(
    string SourcePath,
    string? Nickname,
    int? PatternId = null);
