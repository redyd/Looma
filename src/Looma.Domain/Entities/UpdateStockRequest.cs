namespace Looma.Domain.Entities;

public sealed record UpdateStockRequest(
    int Id,
    int? WoolId,
    double? WeightGrams);
