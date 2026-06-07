// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Looma.Infrastructure.Configurations;

public class WoolConfiguration : IEntityTypeConfiguration<WoolEntity>
{
    public void Configure(EntityTypeBuilder<WoolEntity> builder)
    {
        builder.HasKey(w => w.WoolId);
        builder.Property(w => w.Name).IsRequired();
        builder.Property(w => w.Brand).IsRequired();
        builder.Property(w => w.Material).IsRequired();
        builder.Property(w => w.Color).IsRequired();
        builder.Property(w => w.Weight).IsRequired();
        builder.Property(w => w.Length).IsRequired();
        builder.Property(w => w.Stock).HasDefaultValue(1000).IsRequired();
    }
}