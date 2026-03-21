using Looma.Domain.Entities;
using Looma.Infrastructure.Model;

namespace Looma.Infrastructure.Mapping;

public static class WoolMapping
{
    public static Wool ToDomain(this WoolEntity entity) =>
        Wool.Reconstitute(
            entity.WoolId,
            entity.Name,
            entity.Brand,
            entity.Material,
            entity.Color,
            entity.LengthToWeightRatio
        );

    public static WoolEntity ToEntity(this Wool domain) =>
        new()
        {
            Name = domain.Name,
            Brand = domain.Brand,
            Material = domain.Material,
            Color = domain.Color,
            LengthToWeightRatio = domain.LengthToWeightRatio
        };

    public static void UpdateEntity(this WoolEntity entity, Wool domain)
    {
        entity.Name = domain.Name;
        entity.Brand = domain.Brand;
        entity.Material = domain.Material;
        entity.Color = domain.Color;
        entity.LengthToWeightRatio = domain.LengthToWeightRatio;
    }
}