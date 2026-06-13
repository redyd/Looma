// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Looma.Infrastructure.Configurations;

public class DocumentEntityConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> builder)
    {
        builder.HasKey(d => d.DocumentId);
        builder.Property(d => d.DocumentId).ValueGeneratedNever();
        builder.Property(d => d.Nickname).HasColumnType("TEXT").IsRequired();
        builder.Property(d => d.Type).HasColumnType("TEXT").IsRequired();
        builder.Property(d => d.Size).IsRequired();
        
        builder.HasOne(d => d.Pattern)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.PatternId)
            .IsRequired(false);

        builder.HasOne(d => d.Project)
            .WithMany(p => p.Files)
            .HasForeignKey(d => d.ProjectId)
            .IsRequired(false);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Document_SingleParent",
            "(\"PatternId\" IS NULL OR \"ProjectId\" IS NULL)"
        ));
    }
}
