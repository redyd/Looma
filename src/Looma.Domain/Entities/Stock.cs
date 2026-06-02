namespace Looma.Domain.Entities;

public class Stock
{
    public required int Id { get; init; }
    public required int WoolId { get; init; }
    public required double WeightGrams { get; init; }
}
