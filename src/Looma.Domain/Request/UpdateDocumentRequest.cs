namespace Looma.Domain.Request;

public sealed record UpdateDocumentRequest(
    Guid Id,
    string Nickname);
