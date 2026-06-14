using FluentAssertions;
using Looma.Domain.Core;
using Looma.Infrastructure.Repositories;

namespace Looma.Infrastructure.Tests.Repositories;

public sealed class WoolUsageRepositoryTests
{
    [Fact]
    public async Task GetAllUsagesAsync_returns_project_usages_with_wool_details()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var wool1 = await fixture.AddWoolAsync("A", "Brand", stock: 1000);
        var wool2 = await fixture.AddWoolAsync("B", "Brand", stock: 2000);
        var otherWool = await fixture.AddWoolAsync("Other", "Brand", stock: 3000);
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool1.WoolId, wool2.WoolId]);
        var otherProject = await fixture.AddProjectAsync(pattern.PatternId, [otherWool.WoolId], "Other project");
        await using (var seed = fixture.CreateContext())
        {
            var usage = await seed.WoolsForProjects.FindAsync(wool1.WoolId, project.ProjectId);
            usage!.StockUsed = 100;
            usage.StockAlreadyUsed = 25;
            await seed.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new WoolUsageRepository(context);

        var result = await repository.GetAllUsagesAsync(project.ProjectId);

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Select(u => u.Wool.Id).Should().BeEquivalentTo([wool1.WoolId, wool2.WoolId]);
        result.Value!.Should().ContainSingle(u => u.Wool.Id == wool1.WoolId && u.StockUsed == 100 && u.StockAlreadyUsed == 25);
        result.Value!.Should().NotContain(u => u.Wool.Id == otherWool.WoolId);
        _ = otherProject;
    }

    [Fact]
    public async Task GetUsageAsync_returns_not_found_for_missing_project_wool_pair()
    {
        using var fixture = new RepositoryTestFixture();
        await using var context = fixture.CreateContext();
        var repository = new WoolUsageRepository(context);

        var result = await repository.GetUsageAsync(10, 20);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task UpdateStockUsedAsync_updates_usage_and_clamps_negative_value_to_zero()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var wool = await fixture.AddWoolAsync();
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool.WoolId]);
        await using var context = fixture.CreateContext();
        var repository = new WoolUsageRepository(context);

        (await repository.UpdateStockUsedAsync(project.ProjectId, wool.WoolId, 42)).Succeeded.Should().BeTrue();
        (await repository.UpdateStockUsedAsync(project.ProjectId, wool.WoolId, -10)).Succeeded.Should().BeTrue();

        var stored = await context.WoolsForProjects.FindAsync(wool.WoolId, project.ProjectId);
        stored!.StockUsed.Should().Be(0);
    }

    [Fact]
    public async Task UpdateStockAlreadyUsedAsync_sets_exact_value()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var wool = await fixture.AddWoolAsync();
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool.WoolId]);
        await using var context = fixture.CreateContext();
        var repository = new WoolUsageRepository(context);

        var result = await repository.UpdateStockAlreadyUsedAsync(project.ProjectId, wool.WoolId, 125);

        result.Succeeded.Should().BeTrue(result.Error);
        (await context.WoolsForProjects.FindAsync(wool.WoolId, project.ProjectId))!.StockAlreadyUsed.Should().Be(125);
    }

    [Fact]
    public async Task UpdateCurrentStockUsageAsync_moves_stock_back_to_wool_and_reduces_already_used()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var wool = await fixture.AddWoolAsync(stock: 500);
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool.WoolId]);
        await using (var seed = fixture.CreateContext())
        {
            var usage = await seed.WoolsForProjects.FindAsync(wool.WoolId, project.ProjectId);
            usage!.StockAlreadyUsed = 200;
            await seed.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new WoolUsageRepository(context);

        var result = await repository.UpdateCurrentStockUsageAsync(project.ProjectId, wool.WoolId, 75);

        result.Succeeded.Should().BeTrue(result.Error);
        var storedUsage = await context.WoolsForProjects.FindAsync(wool.WoolId, project.ProjectId);
        storedUsage!.StockAlreadyUsed.Should().Be(125);
        (await context.Wools.FindAsync(wool.WoolId))!.Stock.Should().Be(575);
    }

    [Fact]
    public async Task UpdateCurrentStockUsageAsync_accepts_negative_values_and_clamps_wool_stock_to_zero()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var wool = await fixture.AddWoolAsync(stock: 50);
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool.WoolId]);
        await using var context = fixture.CreateContext();
        var repository = new WoolUsageRepository(context);

        var result = await repository.UpdateCurrentStockUsageAsync(project.ProjectId, wool.WoolId, -100);

        result.Succeeded.Should().BeTrue(result.Error);
        (await context.Wools.FindAsync(wool.WoolId))!.Stock.Should().Be(0);
    }

    [Fact]
    public async Task UpdateCurrentStockUsageAsync_clamps_already_used_to_zero()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var wool = await fixture.AddWoolAsync(stock: 500);
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool.WoolId]);
        await using (var seed = fixture.CreateContext())
        {
            var usage = await seed.WoolsForProjects.FindAsync(wool.WoolId, project.ProjectId);
            usage!.StockAlreadyUsed = 20;
            await seed.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new WoolUsageRepository(context);

        var result = await repository.UpdateCurrentStockUsageAsync(project.ProjectId, wool.WoolId, 100);

        result.Succeeded.Should().BeTrue(result.Error);
        (await context.WoolsForProjects.FindAsync(wool.WoolId, project.ProjectId))!.StockAlreadyUsed.Should().Be(0);
    }
}
