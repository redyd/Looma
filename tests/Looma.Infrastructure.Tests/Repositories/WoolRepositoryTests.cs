using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Tests;

public class WoolRepositoryTests
{
    [Fact]
    public async Task ShouldReturnWoolsOrderedByBrandThenNameWhenGettingAllAsync()
    {
        await using var context = await TestDbContextFactory.CreateSeededContextAsync();
        var repo = new WoolRepository(context);

        var result = await repo.GetAllAsync();

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Select(w => w.Brand + ":" + w.Name)
            .Should().Equal("Drops:Alpaca Silk", "Drops:Merino Extra Fine", "Paintbox:Cotton Light");
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenTheWoolDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var repo = new WoolRepository(context);

        var result = await repo.GetByIdAsync(999);

        result.Succeeded.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain("999");
    }

    [Fact]
    public async Task ShouldCreateAndTrimTheFieldsWhenAddingAWool()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var repo = new WoolRepository(context);

        var result = await repo.AddAsync(new CreateWoolRequest(
            "  Merino  ",
            " Drops ",
            "  Laine  ",
            "  Rouge  ",
            210,
            3,
            5));

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Merino");
        result.Value.Brand.Should().Be("Drops");
        result.Value.Material.Should().Be("Laine");
        result.Value.Color.Should().Be("Rouge");

        context.Wools.Should().HaveCount(1);
        context.Wools.Single().Name.Should().Be("Merino");
    }

    [Fact]
    public async Task ShouldPersistTheChangesWhenUpdatingAnExistingWool()
    {
        await using var context = await TestDbContextFactory.CreateSeededContextAsync();
        var repo = new WoolRepository(context);
        var woolId = await context.Wools.AsNoTracking().Select(w => w.WoolId).FirstAsync();

        var result = await repo.UpdateAsync(new UpdateWoolRequest(
            woolId,
            "  Updated  ",
            null,
            null,
            " Bleu ",
            123,
            null,
            null));

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Updated");
        result.Value.Color.Should().Be("Bleu");
        result.Value.LengthToWeightRatio.Should().Be(123);

        var updated = await context.Wools.AsNoTracking().SingleAsync(w => w.WoolId == woolId);
        updated.Name.Should().Be("Updated");
        updated.Color.Should().Be("Bleu");
        updated.LengthToWeightRatio.Should().Be(123);
    }

    [Fact]
    public async Task ShouldDeleteTheWoolWhenDeletingAnExistingWool()
    {
        await using var context = await TestDbContextFactory.CreateSeededContextAsync();
        var repo = new WoolRepository(context);
        var wool = await context.Wools.FirstAsync();

        var result = await repo.DeleteAsync(wool.WoolId);

        result.Succeeded.Should().BeTrue();
        context.Wools.Should().NotContain(w => w.WoolId == wool.WoolId);
    }
}
