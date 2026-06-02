using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Tests;

public class StockRepositoryTests
{
    [Fact]
    public async Task ShouldReturnTheStocksForAWoolWhenGettingByWoolIdAsync()
    {
        await using var context = await TestDbContextFactory.CreateSeededContextAsync();
        var repo = new StockRepository(context);
        var wool = await context.Wools.FirstAsync();

        var result = await repo.GetByWoolIdAsync(wool.WoolId);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(2);
        result.Value.Sum(s => s.WeightGrams).Should().Be(200);
    }

    [Fact]
    public async Task ShouldReturnTheTotalWeightWhenGettingByWoolIdAsync()
    {
        await using var context = await TestDbContextFactory.CreateSeededContextAsync();
        var repo = new StockRepository(context);
        var wool = await context.Wools.FirstAsync();

        var result = await repo.GetTotalWeightByWoolIdAsync(wool.WoolId);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(200);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenAddingAStockForAMissingWool()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var repo = new StockRepository(context);

        var result = await repo.AddAsync(new CreateStockRequest(123, 50));

        result.Succeeded.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain("123");
    }

    [Fact]
    public async Task ShouldCreateAStockWhenAddingAValidStock()
    {
        await using var context = await TestDbContextFactory.CreateSeededContextAsync();
        var repo = new StockRepository(context);
        var wool = await context.Wools.FirstAsync();

        var result = await repo.AddAsync(new CreateStockRequest(wool.WoolId, 75));

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.WeightGrams.Should().Be(75);
        context.Stocks.Should().ContainSingle(s => Math.Abs(s.WeightQuantity - 75) < 0.01);
    }

    [Fact]
    public async Task ShouldPersistTheChangesWhenUpdatingAnExistingStock()
    {
        await using var context = await TestDbContextFactory.CreateSeededContextAsync();
        var repo = new StockRepository(context);
        var stockId = await context.Stocks.AsNoTracking().Select(s => s.StockId).FirstAsync();

        var result = await repo.UpdateAsync(new UpdateStockRequest(stockId, 333));

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.WeightGrams.Should().Be(333);

        var updated = await context.Stocks.AsNoTracking().SingleAsync(s => s.StockId == stockId);
        updated.WeightQuantity.Should().Be(333);
    }

    [Fact]
    public async Task ShouldDeleteTheStockWhenDeletingAnExistingStock()
    {
        await using var context = await TestDbContextFactory.CreateSeededContextAsync();
        var repo = new StockRepository(context);
        var stock = await context.Stocks.FirstAsync();

        var result = await repo.DeleteAsync(stock.StockId);

        result.Succeeded.Should().BeTrue();
        context.Stocks.Should().NotContain(s => s.StockId == stock.StockId);
    }
}
