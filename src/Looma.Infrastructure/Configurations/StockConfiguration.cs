using Looma.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Looma.Infrastructure.Configurations;

public class StockConfiguration : IEntityTypeConfiguration<StockEntity>
{
    public void Configure(EntityTypeBuilder<StockEntity> builder)
    {
        builder.HasKey(p => p.StockId);
        builder
            .HasOne(s => s.WoolEntity)
            .WithMany(w => w.Stocks)
            .HasForeignKey(s => s.WoolId);
    }
}