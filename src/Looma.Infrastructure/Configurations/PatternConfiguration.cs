// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Looma.Infrastructure.Configurations;

public class PatternConfiguration : IEntityTypeConfiguration<PatternEntity>
{
    public void Configure(EntityTypeBuilder<PatternEntity> builder)
    {
        builder.HasKey(p => p.PatternId);
        builder.Property(p => p.Name).IsRequired();
        builder.Property(p => p.Url).HasColumnType("TEXT");
        builder.Property(p => p.Note).HasColumnType("TEXT");
        builder.Property(p => p.BeginDate).HasColumnType("TEXT");
        builder.Property(p => p.EndDate).HasColumnType("TEXT");
        builder.Property(p => p.IsPersonal).IsRequired();
        builder.Property(p => p.Type).HasConversion<string>().IsRequired();
        
        builder
            .HasMany(p => p.Documents)
            .WithOne(d => d.Pattern);
    }
}
