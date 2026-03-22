namespace Looma.Infrastructure.Model;

public class WoolEntity
{
    public int WoolId { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public string Material { get; set; }
    public string Color { get; set; }
    public double LengthToWeightRatio { get; set; }
    public double NeedleMinSize { get; set; }
    public double NeedleMaxSize { get; set; }

    public ICollection<StockEntity> Stocks { get; set; }
    public ICollection<WoolsForProjectEntity> WoolsForProjects { get; set; }
}