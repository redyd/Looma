namespace Looma.Domain.Entities;

public class Wool
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Brand { get; private set; }
    public string Material { get; private set; }
    public string Color { get; private set; }
    public double LengthToWeightRatio { get; private set; }
    public double NeedleMinSize { get; private set; }
    public double NeedleMaxSize { get; private set; }

    private Wool() { }

    public static Wool Create(
        string name, string brand, string material, string color,
        double lengthToWeightRatio, double needleMinSize, double needleMaxSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(material);
        ArgumentException.ThrowIfNullOrWhiteSpace(color);
        if (lengthToWeightRatio <= 0) throw new ArgumentException("Ratio invalide.");
        if (needleMinSize <= 0 || needleMaxSize <= 0) throw new ArgumentException("Taille d'aiguille invalide.");
        if (needleMinSize > needleMaxSize) throw new ArgumentException("Min doit être inférieur ou égal à Max.");

        return new Wool
        {
            Name = name.Trim(), Brand = brand.Trim(),
            Material = material.Trim(), Color = color,
            LengthToWeightRatio = lengthToWeightRatio,
            NeedleMinSize = needleMinSize, NeedleMaxSize = needleMaxSize
        };
    }

    public static Wool Reconstitute(
        int id, string name, string brand, string material, string color,
        double lengthToWeightRatio, double needleMinSize, double needleMaxSize) =>
        new()
        {
            Id = id, Name = name, Brand = brand, Material = material, Color = color,
            LengthToWeightRatio = lengthToWeightRatio,
            NeedleMinSize = needleMinSize, NeedleMaxSize = needleMaxSize
        };

    public void Update(
        string name, string brand, string material, string color,
        double lengthToWeightRatio, double needleMinSize, double needleMaxSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(material);
        if (lengthToWeightRatio <= 0) throw new ArgumentException("Ratio invalide.");
        if (needleMinSize <= 0 || needleMaxSize <= 0) throw new ArgumentException("Taille d'aiguille invalide.");
        if (needleMinSize > needleMaxSize) throw new ArgumentException("Min doit être inférieur ou égal à Max.");

        Name = name.Trim(); Brand = brand.Trim();
        Material = material.Trim(); Color = color;
        LengthToWeightRatio = lengthToWeightRatio;
        NeedleMinSize = needleMinSize; NeedleMaxSize = needleMaxSize;
    }
}