using Looma.Domain.Entities;
using Looma.Infrastructure.Model;

namespace Looma.Infrastructure.Mapping;

public static class DocumentMapping
{
    public static Document ToDomain(this DocumentEntity entity) =>
        new()
        {
            Id = entity.DocumentId,
            Nickname = entity.Nickname ?? string.Empty,
            Type = string.Empty,
            SizeBytes = 0
        };

    public static DocumentEntity ToEntity(this Document document) =>
        new()
        {
            DocumentId = document.Id,
            Nickname = document.Nickname
        };
}
