using Looma.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Looma.Infrastructure;

public class LoomaDbContextFactory : IDesignTimeDbContextFactory<LoomaDbContext>
{
    public LoomaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LoomaDbContext>()
            .UseSqlite($"Data Source={AppPaths.DatabasePath}")
            .Options;

        return new LoomaDbContext(options);
    }
}