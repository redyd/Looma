using Looma.Infrastructure;
using Looma.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Tests;

internal static class TestDbContextFactory
{
    public static LoomaDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<LoomaDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new LoomaDbContext(options);
    }

    public static async Task<LoomaDbContext> CreateSeededContextAsync()
    {
        var context = CreateContext();

        var wool1 = new WoolEntity
        {
            Name = "Alpaca Silk",
            Brand = "Drops",
            Material = "Alpaca",
            Color = "Beige",
            LengthToWeightRatio = 400,
            NeedleMinSize = 2,
            NeedleMaxSize = 5,
            Stocks = [],
            WoolsForProjects = []
        };

        var wool2 = new WoolEntity
        {
            Name = "Merino Extra Fine",
            Brand = "Drops",
            Material = "Mérinos",
            Color = "Rouge",
            LengthToWeightRatio = 200,
            NeedleMinSize = 2.5,
            NeedleMaxSize = 5,
            Stocks = [],
            WoolsForProjects = []
        };

        var wool3 = new WoolEntity
        {
            Name = "Cotton Light",
            Brand = "Paintbox",
            Material = "Coton",
            Color = "Bleu",
            LengthToWeightRatio = 350,
            NeedleMinSize = 2.5,
            NeedleMaxSize = 5,
            Stocks = [],
            WoolsForProjects = []
        };

        context.Wools.AddRange(wool1, wool2, wool3);
        await context.SaveChangesAsync();

        context.Stocks.AddRange(
            new StockEntity { WoolId = wool1.WoolId, WeightQuantity = 150 },
            new StockEntity { WoolId = wool1.WoolId, WeightQuantity = 50 },
            new StockEntity { WoolId = wool2.WoolId, WeightQuantity = 200 }
        );

        await context.SaveChangesAsync();
        return context;
    }
}
