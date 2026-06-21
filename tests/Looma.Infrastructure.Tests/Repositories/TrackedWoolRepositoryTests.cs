// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
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
}
