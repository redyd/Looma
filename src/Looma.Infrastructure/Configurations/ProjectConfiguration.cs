using Looma.Infrastructure.Model;
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