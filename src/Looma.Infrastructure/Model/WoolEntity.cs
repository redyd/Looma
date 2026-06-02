namespace Looma.Infrastructure.Model;

public class WoolEntity
{
    public int WoolId { get; set; }
    public string Name { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Material { get; set; } = null!;
    public string Color { get; set; } = null!;
    public double LengthToWeightRatio { get; set; }
    public double NeedleMinSize { get; set; }
    public double NeedleMaxSize { get; set; }

    public ICollection<StockEntity> Stocks { get; set; } = new  List<StockEntity>();
    public ICollection<WoolsForProjectEntity> WoolsForProjects { get; set; } = new List<WoolsForProjectEntity>();
}