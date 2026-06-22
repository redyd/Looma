// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Infrastructure.Entity;
using Looma.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Looma.Infrastructure.Tests.Repositories;

public sealed class TrackedWoolRepositoryTests
{
    [Fact]
    public async Task AddAsync_Persists_Tracked_Wool_With_Project()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync();
        var wool = await fixture.AddWoolAsync();
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool.WoolId]);

        await using var context = fixture.CreateContext();
        var repository = new TrackedWoolRepository(context);

        var result = await repository.AddAsync(wool.WoolId, -250, project.ProjectId);

        result.Succeeded.Should().BeTrue(result.Error);
        var tracked = await context.TrackedWools.SingleAsync();
        tracked.Id.Should().NotBeNullOrWhiteSpace();
        tracked.WoolId.Should().Be(wool.WoolId);
        tracked.ProjectId.Should().Be(project.ProjectId);
        tracked.Quantity.Should().Be(-250);
        tracked.Date.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetMovementsAsync_Returns_Filtered_Domain_Movements_With_Project_And_Pattern()
    {
        using var fixture = new RepositoryTestFixture();
        var crochet = await fixture.AddPatternAsync("Crochet pattern");
        var tricot = await fixture.AddPatternAsync("Tricot pattern", Looma.Domain.Core.PatternType.Tricot);
        var wool = await fixture.AddWoolAsync("Alpaca", "Drops");
        var oldProject = await fixture.AddProjectAsync(crochet.PatternId, [wool.WoolId], "Old project");
        var recentProject = await fixture.AddProjectAsync(tricot.PatternId, [wool.WoolId], "Recent project");

        await using (var seedContext = fixture.CreateContext())
        {
            seedContext.TrackedWools.AddRange(
                new TrackedWool
                {
                    Id = "old",
                    Date = new DateTime(2025, 12, 31, 10, 0, 0, DateTimeKind.Utc),
                    Quantity = -50,
                    WoolId = wool.WoolId,
                    ProjectId = oldProject.ProjectId
                },
                new TrackedWool
                {
                    Id = "recent",
                    Date = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                    Quantity = -120,
                    WoolId = wool.WoolId,
                    ProjectId = recentProject.ProjectId
                },
                new TrackedWool
                {
                    Id = "adjustment",
                    Date = new DateTime(2026, 1, 12, 10, 0, 0, DateTimeKind.Utc),
                    Quantity = 40,
                    WoolId = wool.WoolId
                });
            await seedContext.SaveChangesAsync();
        }

        await using var context = fixture.CreateContext();
        var repository = new TrackedWoolRepository(context);

        var result = await repository.GetMovementsAsync(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        result.Succeeded.Should().BeTrue(result.Error);
        result.Value.Should().NotBeNull();
        result.Value!.Select(m => m.Id).Should().Equal("recent", "adjustment");
        var recent = result.Value!.First(m => m.Id == "recent");
        recent.WoolName.Should().Be("Alpaca");
        recent.WoolBrand.Should().Be("Drops");
        recent.ProjectName.Should().Be("Recent project");
        recent.PatternType.Should().Be(Looma.Domain.Core.PatternType.Tricot);
        result.Value!.First(m => m.Id == "adjustment").ProjectId.Should().BeNull();
    }
}
