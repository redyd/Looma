namespace Looma.Domain.Entities;

public sealed record UpdateDocumentRequest(
    Guid Id,
    string Nickname);
