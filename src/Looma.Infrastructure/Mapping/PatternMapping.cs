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
            BeginDate = entity.BeginDate,
            EndDate = entity.EndDate,
            Documents = entity.Documents.Select(d => d.ToDomain()).ToList(),
            Projects = entity.Projects.Select(p => p.ToDomain()).ToList(),
            IsPersonal = entity.IsPersonal,
            Type = entity.Type,
        };

    public static PatternProject ToDomain(this ProjectEntity entity) =>
        new()
        {
            Id = entity.ProjectId,
            Name = entity.Name,
            Status = entity.Status
        };
}
