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
        var trackedRepository = Substitute.For<ITrackedWoolRepository>();
        var sut = new WoolService(repository, trackedRepository, Substitute.For<IDomainLogger>());
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
        var trackedRepository = Substitute.For<ITrackedWoolRepository>();
        repository.UpdateAsync(Arg.Any<UpdateWoolRequest>()).Returns(ResultT<Looma.Domain.Entities.Wool>.Ok());
        var sut = new WoolService(repository, trackedRepository, Substitute.For<IDomainLogger>());
        var request = new UpdateWoolRequest(12, "Sock", "Drops", "Wool", [], 50, 170, 3.25, 3.75);

        var result = await sut.UpdateAsync(request);

        result.Succeeded.Should().BeTrue(result.Error);
        await repository.Received(1).UpdateAsync(request);
    }

    [Fact]
    public async Task AddStockAsync_WhenStockUpdateSucceeds_TracksQuantityWithProject()
    {
        var repository = Substitute.For<IWoolRepository>();
        var trackedRepository = Substitute.For<ITrackedWoolRepository>();
        repository.GetByIdAsync(12).Returns(ResultT<Looma.Domain.Entities.Wool>.Ok(ValidWool(stock: 1000)));
        repository.AddStock(12, -250).Returns(Result.Ok());
        trackedRepository.AddAsync(12, -250, 7).Returns(Result.Ok());
        var sut = new WoolService(repository, trackedRepository, Substitute.For<IDomainLogger>());

        var result = await sut.AddStockAsync(12, -250, 7);

        result.Succeeded.Should().BeTrue(result.Error);
        await repository.Received(1).AddStock(12, -250);
        await trackedRepository.Received(1).AddAsync(12, -250, 7);
    }

    [Fact]
    public async Task AddStockAsync_WhenStockIsClamped_TracksActualStockDelta()
    {
        var repository = Substitute.For<IWoolRepository>();
        var trackedRepository = Substitute.For<ITrackedWoolRepository>();
        repository.GetByIdAsync(12).Returns(ResultT<Looma.Domain.Entities.Wool>.Ok(ValidWool(stock: 100)));
        repository.AddStock(12, -250).Returns(Result.Ok());
        trackedRepository.AddAsync(12, -100, null).Returns(Result.Ok());
        var sut = new WoolService(repository, trackedRepository, Substitute.For<IDomainLogger>());

        var result = await sut.AddStockAsync(12, -250);

        result.Succeeded.Should().BeTrue(result.Error);
        await trackedRepository.Received(1).AddAsync(12, -100, null);
    }

    private static CreateWoolRequest ValidCreateRequest() =>
        new("Sock", "Drops", "Wool", [], 50, 170, 1000, 3, 3.75);

    private static Looma.Domain.Entities.Wool ValidWool(double stock) => new()
    {
        Id = 12,
        Name = "Sock",
        Brand = "Drops",
        Material = "Wool",
        Colors = [],
        Weight = 50,
        Length = 170,
        Stock = stock,
        NeedleMinSize = 3,
        NeedleMaxSize = 3.75
    };
}
