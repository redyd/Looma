using Looma.Domain.Core;
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
            Documents = entity.Documents.Select(d => d.ToDomain()).ToList(),
            Projects = entity.Projects.Select(p => p.ToDomain()).ToList()
        };

    public static PatternEntity ToEntity(this Pattern domain) =>
        new()
        {
            PatternId = domain.Id,
            Name = domain.Name,
            Url = domain.Url,
            Note = domain.Note
        };

    public static PatternProject ToDomain(this ProjectEntity entity) =>
        new()
        {
            Id = entity.ProjectId,
            Name = entity.Name,
            Status = entity.Status
        };
}
