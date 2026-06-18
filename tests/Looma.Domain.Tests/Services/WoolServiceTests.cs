// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;
using Looma.Domain.Request;
using Looma.Domain.Services;
using NSubstitute;

namespace Looma.Domain.Tests.Services;

public sealed class WoolServiceTests
{
    [Fact]
    public async Task AddAsync_Rejects_Unknown_Needle_Range_And_Does_Not_Call_Repository()
    {
        var repository = Substitute.For<IWoolRepository>();
        var sut = new WoolService(repository, Substitute.For<IDomainLogger>());
        var request = ValidCreateRequest() with { NeedleMinSize = 3, NeedleMaxSize = 4 };

        var result = await sut.AddAsync(request);

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("La taille d'aiguilles doit correspondre à une plage de laine connue.");
        await repository.DidNotReceive().AddAsync(Arg.Any<CreateWoolRequest>());
    }

    [Fact]
    public async Task UpdateAsync_Accepts_Known_Needle_Range()
    {
        var repository = Substitute.For<IWoolRepository>();
        repository.UpdateAsync(Arg.Any<UpdateWoolRequest>()).Returns(ResultT<Looma.Domain.Entities.Wool>.Ok());
        var sut = new WoolService(repository, Substitute.For<IDomainLogger>());
        var request = new UpdateWoolRequest(12, "Sock", "Drops", "Wool", [], 50, 170, 3.25, 3.75);

        var result = await sut.UpdateAsync(request);

        result.Succeeded.Should().BeTrue(result.Error);
        await repository.Received(1).UpdateAsync(request);
    }

    private static CreateWoolRequest ValidCreateRequest() =>
        new("Sock", "Drops", "Wool", [], 50, 170, 1000, 3, 3.75);
}
