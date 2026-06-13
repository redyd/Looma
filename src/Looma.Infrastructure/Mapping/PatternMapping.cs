// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Entities;
using Looma.Infrastructure.Entity;

namespace Looma.Infrastructure.Mapping;

public static class PatternMapping
{
    public static Pattern ToDomain(this PatternEntity entity) =>
        new()
        {
            Id = entity.PatternId,
            Name = entity.Name,
            Url = entity.Url,
            Note = entity.Note,
            BeginDate = entity.BeginDate,
            EndDate = entity.EndDate,
            Documents = entity.Documents.Select(d => d.ToDomain()).ToList(),
            Projects = entity.Projects.Select(p => p.ToPatternProjectDomain()).ToList(),
            IsPersonal = entity.IsPersonal,
            Type = entity.Type,
        };

    public static PatternProject ToPatternProjectDomain(this ProjectEntity entity) =>
        new()
        {
            Id = entity.ProjectId,
            Name = entity.Name,
            Status = entity.Status
        };
}
