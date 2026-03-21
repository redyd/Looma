namespace Looma.Domain.Entities;

public class Wool
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Brand { get; private set; }
    public string Material { get; private set; }
    public string Color { get; private set; }
    public double LengthToWeightRatio { get; private set; }

    private Wool() { }

    public static Wool Create(
        string name,
        string brand,
        string material,
        string color,
        double lengthToWeightRatio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(material);
        ArgumentException.ThrowIfNullOrWhiteSpace(color);

        if (lengthToWeightRatio <= 0)
            throw new ArgumentException("Le ratio longueur/poids doit être positif.", nameof(lengthToWeightRatio));

        return new Wool
        {
            Name = name.Trim(),
            Brand = brand.Trim(),
            Material = material.Trim(),
            Color = color.Trim(),
            LengthToWeightRatio = lengthToWeightRatio
        };
    }

    public static Wool Reconstitute(
        int id,
        string name,
        string brand,
        string material,
        string color,
        double lengthToWeightRatio) =>
        new()
        {
            Id = id,
            Name = name,
            Brand = brand,
            Material = material,
            Color = color,
            LengthToWeightRatio = lengthToWeightRatio
        };

    public void Update(
        string name,
        string brand,
        string material,
        string color,
        double lengthToWeightRatio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(material);
        ArgumentException.ThrowIfNullOrWhiteSpace(color);

        if (lengthToWeightRatio <= 0)
            throw new ArgumentException("Le ratio longueur/poids doit être positif.", nameof(lengthToWeightRatio));

        Name = name.Trim();
        Brand = brand.Trim();
        Material = material.Trim();
        Color = color.Trim();
        LengthToWeightRatio = lengthToWeightRatio;
    }
}