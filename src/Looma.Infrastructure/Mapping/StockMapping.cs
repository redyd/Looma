using Looma.Domain.Entities;
using Looma.Infrastructure.Model;

namespace Looma.Infrastructure.Mapping;

public static class StockMapping
{
    public static Stock ToDomain(this StockEntity e) =>
        Stock.Reconstitute(e.StockId, e.WoolId, e.WeightQuantity);

    public static StockEntity ToEntity(this Stock s) =>
        new() { WoolId = s.WoolId, WeightQuantity = s.WeightGrams };

    public static void UpdateEntity(this StockEntity e, Stock s) =>
        e.WeightQuantity = s.WeightGrams;
}