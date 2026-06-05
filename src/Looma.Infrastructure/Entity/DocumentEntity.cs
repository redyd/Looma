namespace Looma.Infrastructure.Entity;

public class DocumentEntity
{
    public Guid DocumentId { get; set; }
    public string Nickname { get; set; } = null!;
    public PatternEntity Pattern = null!;
}
