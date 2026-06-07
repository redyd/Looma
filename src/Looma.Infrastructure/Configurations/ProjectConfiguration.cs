// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Looma.Infrastructure.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    public void Configure(EntityTypeBuilder<ProjectEntity> builder)
    {
        builder.HasKey(p => p.ProjectId);
        builder.Property(p => p.Name).IsRequired();
        builder
            .HasOne(p => p.PatternEntity)
            .WithMany(p => p.Projects)
            .HasForeignKey(p => p.PatronId);
    }
}