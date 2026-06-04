using Looma.Domain.Entities;
using Looma.Infrastructure.Entity;

namespace Looma.Infrastructure.Mapping;

public static class DocumentMapping
{
    public static Document ToDomain(this DocumentEntity entity) =>
        new()
        {
            Id = entity.DocumentId,
            Nickname = entity.Nickname,
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
