namespace Looma.Infrastructure.Model;

public class StockEntity
{
    public int StockId { get; set; }
    public double WeightQuantity { get; set; }

    public int WoolId { get; set; }
    public WoolEntity WoolEntity { get; set; }
}