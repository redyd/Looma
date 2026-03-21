using Looma.Domain.Core;

namespace Looma.Infrastructure.Model;

public class ProjectEntity
{
    public int ProjectId { get; set; }
    public string Name { get; set; }
    public DateOnly? BeginDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public int PatronId { get; set; }
    public PatternEntity PatternEntity { get; set; }

    public Status Status { get; set; }

    public ICollection<WoolsForProjectEntity> WoolsForProjects { get; set; } = new List<WoolsForProjectEntity>();
}
