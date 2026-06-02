using Looma.Domain.Entities;
using Looma.Infrastructure.Model;

namespace Looma.Infrastructure.Mapping;

public static class WoolMapping
{
    public static Wool ToDomain(this WoolEntity entity) =>
        new()
        {
            Id = entity.WoolId,
            Name = entity.Name,
            Brand = entity.Brand,
            Material = entity.Material,
            Color = entity.Color,
            LengthToWeightRatio = entity.LengthToWeightRatio,
            NeedleMinSize = entity.NeedleMinSize,
            NeedleMaxSize = entity.NeedleMaxSize
        };

    public static WoolEntity ToEntity(this Wool domain) =>
        new()
        {
            Name = domain.Name,
            Brand = domain.Brand,
            Material = domain.Material,
            Color = domain.Color,
            LengthToWeightRatio = domain.LengthToWeightRatio,
            NeedleMinSize = domain.NeedleMinSize,
            NeedleMaxSize = domain.NeedleMaxSize
        };
}