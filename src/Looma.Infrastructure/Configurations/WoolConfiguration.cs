using Looma.Infrastructure.Model;
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
    }
}