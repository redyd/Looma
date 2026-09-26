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

    [Fact]
    public async Task AddAsync_Stores_Wool_And_Project_Snapshot()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync(type: Looma.Domain.Core.PatternType.Tricot);
        var wool = await fixture.AddWoolAsync("Alpaca", "Drops");
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool.WoolId], "Pull");

        await using var context = fixture.CreateContext();
        var result = await new TrackedWoolRepository(context).AddAsync(wool.WoolId, -250, project.ProjectId);

        result.Succeeded.Should().BeTrue(result.Error);
        var tracked = await context.TrackedWools.SingleAsync();
        tracked.WoolName.Should().Be("Alpaca");
        tracked.WoolBrand.Should().Be("Drops");
        tracked.WoolMaterial.Should().Be(wool.Material);
        tracked.WoolColor.Should().Be(wool.Color);
        tracked.WoolWeight.Should().Be(wool.Weight);
        tracked.WoolLength.Should().Be(wool.Length);
        tracked.WoolNeedleMinSize.Should().Be(wool.NeedleMinSize);
        tracked.WoolNeedleMaxSize.Should().Be(wool.NeedleMaxSize);
        tracked.ProjectName.Should().Be("Pull");
        tracked.PatternType.Should().Be(Looma.Domain.Core.PatternType.Tricot);
    }

    [Fact]
    public async Task Deleting_Wool_And_Project_Keeps_Movement_History()
    {
        using var fixture = new RepositoryTestFixture();
        var pattern = await fixture.AddPatternAsync(type: Looma.Domain.Core.PatternType.Crochet);
        var wool = await fixture.AddWoolAsync("Mohair", "Archive");
        var project = await fixture.AddProjectAsync(pattern.PatternId, [wool.WoolId], "Châle");

        await using (var context = fixture.CreateContext())
        {
            var added = await new TrackedWoolRepository(context).AddAsync(wool.WoolId, -1_500, project.ProjectId);
            added.Succeeded.Should().BeTrue(added.Error);
        }

        await using (var context = fixture.CreateContext())
        {
            (await new ProjectRepository(context, fixture.Paths).DeleteAsync(project.ProjectId)).Succeeded.Should().BeTrue();
        }

        await using (var context = fixture.CreateContext())
        {
            (await new WoolRepository(context).DeleteAsync(wool.WoolId)).Succeeded.Should().BeTrue();
        }

        await using var readContext = fixture.CreateContext();
        var result = await new TrackedWoolRepository(readContext).GetMovementsAsync();

        result.Succeeded.Should().BeTrue(result.Error);
        var movement = result.Value!.Should().ContainSingle().Subject;
        movement.WoolId.Should().BeNull();
        movement.WoolExists.Should().BeFalse();
        movement.WoolName.Should().Be("Mohair");
        movement.WoolBrand.Should().Be("Archive");
        movement.WoolWeight.Should().Be(wool.Weight);
        movement.ProjectId.Should().BeNull();
        movement.ProjectName.Should().Be("Châle");
        movement.PatternType.Should().Be(Looma.Domain.Core.PatternType.Crochet);
        movement.Quantity.Should().Be(-1_500);
    }
}
