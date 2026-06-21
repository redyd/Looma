namespace Looma.Infrastructure.Entity;

public class TrackedWool
{
    public string Id { get; set; } = null!;
    public DateTime Date { get; set; }
    public double Quantity { get; set; }

    public int WoolId { get; set; }
    public WoolEntity WoolEntity { get; set; } = null!;

    public int? ProjectId { get; set; }
    public ProjectEntity? ProjectEntity { get; set; } = null!;
}