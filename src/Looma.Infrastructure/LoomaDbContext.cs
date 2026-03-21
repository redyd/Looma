using Looma.Infrastructure.Configurations;
using Looma.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure;

public class LoomaDbContext : DbContext
{
    public DbSet<WoolEntity> Wools => Set<WoolEntity>();
    public DbSet<StockEntity> Stocks => Set<StockEntity>();
    public DbSet<PatternEntity> Patterns => Set<PatternEntity>();
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<WoolsForProjectEntity> WoolsForProjects => Set<WoolsForProjectEntity>();

    public LoomaDbContext(DbContextOptions<LoomaDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(new WoolForProjectConfiguration())
            .ApplyConfiguration(new StockConfiguration())
            .ApplyConfiguration(new ProjectConfiguration())
            .ApplyConfiguration(new DocumentConfiguration())
            .ApplyConfiguration(new WoolConfiguration())
            .ApplyConfiguration(new PatternConfiguration());
    }
}