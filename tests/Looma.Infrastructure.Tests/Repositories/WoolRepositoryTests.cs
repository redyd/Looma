// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Request;
using Looma.Infrastructure.Repositories;

namespace Looma.Infrastructure.Tests.Repositories;

public sealed class WoolRepositoryTests
{
    [Fact]
    public async Task AddAsync_trims_and_persists_valid_wool()
    {
        using var fixture = new RepositoryTestFixture();
        await using var context = fixture.CreateContext();
        var repository = new WoolRepository(context);

        var result = await repository.AddAsync(new CreateWoolRequest(
            "  Alpaca  ", "  Drops  ", "  Alpaca  ", ["  Green  "], 50, 150, 2500, 3, 4.5));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Alpaca");
        result.Value.Brand.Should().Be("Drops");
        result.Value.Stock.Should().Be(2500);
        context.Wools.Single().Name.Should().Be("Alpaca");
    }

    [Theory]
    [InlineData("", "Brand", "Material", "Color", 50, 100, 1000, 3, 4)]
    [InlineData("Name", "", "Material", "Color", 50, 100, 1000, 3, 4)]
    [InlineData("Name", "Brand", "", "Color", 50, 100, 1000, 3, 4)]
    [InlineData("Name", "Brand", "Material", "", 50, 100, 1000, 3, 4)]
    [InlineData("Name", "Brand", "Material", "Color", 0, 100, 1000, 3, 4)]
    [InlineData("Name", "Brand", "Material", "Color", 50, 0, 1000, 3, 4)]
    [InlineData("Name", "Brand", "Material", "Color", 50, 100, -1, 3, 4)]
    [InlineData("Name", "Brand", "Material", "Color", 50, 100, 1000, 0, 4)]
    [InlineData("Name", "Brand", "Material", "Color", 50, 100, 1000, 5, 4)]
    public async Task AddAsync_rejects_invalid_create_data(
        string name,
        string brand,
        string material,
        string color,
        double weight,
        double length,
        double stock,
        double needleMin,
        double needleMax)
    {
        using var fixture = new RepositoryTestFixture();
        await using var context = fixture.CreateContext();
        var repository = new WoolRepository(context);

        var result = await repository.AddAsync(new CreateWoolRequest(
            name, brand, material, [color], weight, length, stock, needleMin, needleMax));

        result.Status.Should().Be(ResultStatus.Failure);
        context.Wools.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_returns_wools_ordered_by_brand_then_name()
    {
        using var fixture = new RepositoryTestFixture();
        await fixture.AddWoolAsync("Zulu", "Beta");
        await fixture.AddWoolAsync("Alpha", "Beta");
        await fixture.AddWoolAsync("Middle", "Alpha");
        await using var context = fixture.CreateContext();
        var repository = new WoolRepository(context);

        var result = await repository.GetAllAsync();

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Select(w => $"{w.Brand}:{w.Name}")
            .Should().Equal("Alpha:Middle", "Beta:Alpha", "Beta:Zulu");
    }

    [Fact]
    public async Task UpdateAsync_updates_fields_without_changing_stock()
    {
        using var fixture = new RepositoryTestFixture();
        var wool = await fixture.AddWoolAsync(stock: 3210);
        await using var context = fixture.CreateContext();
        var repository = new WoolRepository(context);

        var result = await repository.UpdateAsync(fixture.ValidUpdateWoolRequest(wool.WoolId, "  New name  "));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value!.Name.Should().Be("New name");
        result.Value.Stock.Should().Be(3210);
    }

    [Fact]
    public async Task AddStock_adds_quantity_and_clamps_to_zero()
    {
        using var fixture = new RepositoryTestFixture();
        var wool = await fixture.AddWoolAsync(stock: 100);
        await using var context = fixture.CreateContext();
        var repository = new WoolRepository(context);

        (await repository.AddStock(wool.WoolId, 50)).Succeeded.Should().BeTrue();
        (await repository.AddStock(wool.WoolId, -200)).Succeeded.Should().BeTrue();

        var stored = await context.Wools.FindAsync(wool.WoolId);
        stored!.Stock.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_removes_existing_wool()
    {
        using var fixture = new RepositoryTestFixture();
        var wool = await fixture.AddWoolAsync();
        await using var context = fixture.CreateContext();
        var repository = new WoolRepository(context);

        var result = await repository.DeleteAsync(wool.WoolId);

        result.Succeeded.Should().BeTrue(result.Error);
        context.Wools.Should().BeEmpty();
    }
}
