// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

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
