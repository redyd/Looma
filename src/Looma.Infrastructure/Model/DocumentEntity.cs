namespace Looma.Infrastructure.Model;

public class DocumentEntity
{
    public int DocumentId { get; set; }
    public string RelativePath { get; set; } = null!;
    public string? Nickname { get; set; }
    
    public ICollection<PatternEntity> Patterns { get; set; } = new List<PatternEntity>();
}