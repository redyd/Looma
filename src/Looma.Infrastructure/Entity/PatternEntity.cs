using Looma.Domain.Core;

namespace Looma.Infrastructure.Entity;

public class PatternEntity
{
    public int PatternId { get; set; }
    public string Name { get; set; } = null!;
    public string? Url { get; set; }
    public string? Note { get; set; }
    public DateOnly? BeginDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public PatternType Type { get; set; }
    public bool IsPersonal { get; set; }
    
    // RELATIONS
    public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
    public ICollection<ProjectEntity> Projects { get; set; } = new List<ProjectEntity>();
}
