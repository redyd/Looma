namespace Looma.Domain.Entities;

public class WoolUsage
{
    public required Wool Wool { get; init; }
    public required double StockUsed { get; init; }

    public double RemainingStock => Wool.Stock - StockUsed;
}