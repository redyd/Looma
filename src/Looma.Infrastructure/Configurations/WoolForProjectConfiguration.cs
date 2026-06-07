// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Looma.Infrastructure.Configurations;

public class WoolForProjectConfiguration : IEntityTypeConfiguration<WoolsForProjectEntity>
{
    public void Configure(EntityTypeBuilder<WoolsForProjectEntity> builder)
    {
        builder
            .HasKey(w => new { w.WoolId, w.ProjectId });

        builder
            .HasOne(w => w.WoolEntity)
            .WithMany(w => w.WoolsForProjects)
            .HasForeignKey(w => w.WoolId);

        builder
            .HasOne(w => w.ProjectEntity)
            .WithMany(p => p.WoolsForProjects)
            .HasForeignKey(w => w.ProjectId);
    }
}