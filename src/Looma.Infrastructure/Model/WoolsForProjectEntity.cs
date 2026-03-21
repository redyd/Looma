namespace Looma.Infrastructure.Model;

public class WoolsForProjectEntity
{
    public int WoolId { get; set; }
    public WoolEntity WoolEntity { get; set; }

    public int ProjectId { get; set; }
    public ProjectEntity ProjectEntity { get; set; }
}
