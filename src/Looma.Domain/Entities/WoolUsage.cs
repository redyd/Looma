namespace Looma.Domain.Entities;

public class WoolUsage
{
    public required Wool Wool { get; init; }
    public required double StockUsed { get; init; }
    public required double StockAlreadyUsed { get; init; }

    public double RemainingStock => Wool.Stock;
}
