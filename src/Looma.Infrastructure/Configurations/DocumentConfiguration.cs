using Looma.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Looma.Infrastructure.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> builder)
    {
        builder.HasKey(d => d.DocumentId);
        builder.Property(d => d.RelativePath).IsRequired();
        builder
            .HasMany(d => d.Patterns)
            .WithMany(p => p.Documents)
            .UsingEntity(j => j.ToTable("DocumentPattern"));
    }
}