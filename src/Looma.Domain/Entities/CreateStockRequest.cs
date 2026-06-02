namespace Looma.Domain.Entities;

public sealed record CreateStockRequest(
    int WoolId,
    double WeightGrams);