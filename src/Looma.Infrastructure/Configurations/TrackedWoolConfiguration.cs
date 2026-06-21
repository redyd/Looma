// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Looma.Infrastructure.Configurations;

public class TrackedWoolConfiguration : IEntityTypeConfiguration<TrackedWool>
{
    public void Configure(EntityTypeBuilder<TrackedWool> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).IsRequired();
        builder.Property(t => t.Date).IsRequired();
        builder.Property(t => t.Quantity).IsRequired();

        builder
            .HasOne(t => t.WoolEntity)
            .WithMany(w => w.TrackedWools)
            .HasForeignKey(t => t.WoolId);

        builder
            .HasOne(t => t.ProjectEntity)
            .WithMany(p => p.TrackedWools)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
