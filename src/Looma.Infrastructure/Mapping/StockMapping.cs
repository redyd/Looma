using Looma.Domain.Entities;
using Looma.Infrastructure.Model;

namespace Looma.Infrastructure.Mapping;

public static class StockMapping
{
    public static Stock ToDomain(this StockEntity e) =>
        new()
        {
            Id = e.StockId,
            WoolId = e.WoolId,
            WeightGrams = e.WeightQuantity
        };

    public static StockEntity ToEntity(this Stock s) =>
        new() { WoolId = s.WoolId, WeightQuantity = s.WeightGrams };
}
