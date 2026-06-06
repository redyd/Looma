using Looma.Infrastructure.Configurations;
using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure;

public class LoomaDbContext(DbContextOptions<LoomaDbContext> options) : DbContext(options)
{
    public DbSet<WoolEntity> Wools => Set<WoolEntity>();
    public DbSet<PatternEntity> Patterns => Set<PatternEntity>();
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<WoolsForProjectEntity> WoolsForProjects => Set<WoolsForProjectEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(new WoolForProjectConfiguration())
            .ApplyConfiguration(new ProjectConfiguration())
            .ApplyConfiguration(new DocumentConfiguration())
            .ApplyConfiguration(new WoolConfiguration())
            .ApplyConfiguration(new PatternConfiguration());
    }
}