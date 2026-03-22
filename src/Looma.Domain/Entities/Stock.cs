namespace Looma.Domain.Entities;

public class Stock
{
    public int Id { get; private set; }
    public int WoolId { get; private set; }
    public double WeightGrams { get; private set; }

    private Stock() { }

    public static Stock Create(int woolId, double weightGrams)
    {
        if (weightGrams <= 0)
            throw new ArgumentException("Le poids doit être positif.", nameof(weightGrams));

        return new Stock { WoolId = woolId, WeightGrams = weightGrams };
    }

    public static Stock Reconstitute(int id, int woolId, double weightGrams) =>
        new() { Id = id, WoolId = woolId, WeightGrams = weightGrams };

    public void UpdateWeight(double weightGrams)
    {
        if (weightGrams <= 0)
            throw new ArgumentException("Le poids doit être positif.", nameof(weightGrams));
        WeightGrams = weightGrams;
    }
}