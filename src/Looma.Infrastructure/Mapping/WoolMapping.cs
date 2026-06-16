// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Entities;
using Looma.Infrastructure.Entity;

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
            Colors = entity.Color.Split("|").Select(c => c.Trim()).Where(e => e.Length > 0).ToList(),
            Length = entity.Length,
            Weight = entity.Weight,
            Stock = entity.Stock,
            NeedleMinSize = entity.NeedleMinSize,
            NeedleMaxSize = entity.NeedleMaxSize
        };

    public static WoolEntity ToEntity(this Wool domain) =>
        new()
        {
            Name = domain.Name,
            Brand = domain.Brand,
            Material = domain.Material,
            Color = string.Join("|", domain.Colors),
            Length = domain.Length,
            Weight = domain.Weight,
            Stock = domain.Stock,
            NeedleMinSize = domain.NeedleMinSize,
            NeedleMaxSize = domain.NeedleMaxSize
        };
}