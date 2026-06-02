namespace Looma.Domain.Entities;

public sealed record UpdateStockRequest(
    int Id,
    double WeightGrams);