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
        builder
            .HasMany(p => p.Documents)
            .WithOne(d => d.Pattern);
    }
}
