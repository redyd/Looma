namespace Looma.Infrastructure.Model;

public class DocumentEntity
{
    public Guid DocumentId { get; set; }
    public string Nickname { get; set; } = null!;
    public ICollection<PatternEntity> Patterns { get; set; } = new List<PatternEntity>();
}
