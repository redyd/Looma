// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.IServices;
using Looma.Domain.Repositories;
using Looma.Domain.Request;
using Looma.Domain.Services;
using NSubstitute;

namespace Looma.Domain.Tests.Services;

public class ProjectServiceTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static Wool MakeWool(int id = 1, string name = "Wool", double stock = 5000) => new()
    {
        Id = id,
        Name = name,
        Brand = "Brand",
        Material = "Wool",
        Colors = ["Red"],
        Weight = 100,
        Length = 200,
        Stock = stock,
        NeedleMinSize = 4,
        NeedleMaxSize = 6
    };

    private static WoolUsage MakeWoolUsage(Wool? wool = null, double stockUsed = 0, double stockAlreadyUsed = 0) => new()
    {
        Wool = wool ?? MakeWool(),
        StockUsed = stockUsed,
        StockAlreadyUsed = stockAlreadyUsed
    };

    private static Pattern MakePattern() => new()
    {
        Id = 1,
        Name = "Pattern",
        Documents = [],
        BeginDate = null,
        EndDate = null,
        Type = PatternType.Crochet,
        Projects = [],
        IsPersonal = false,
        Url = null,
        Note = null,
    };

    private static Project MakeProject(
        int id = 1,
        Status status = Status.InProgress,
        IReadOnlyList<WoolUsage>? wools = null) => new()
        {
            ProjectId = id,
            Name = "Test Project",
            Status = status,
            Note = null,
            BeginDate = null,
            EndDate = null,
            Pattern = MakePattern(),
            Wools = wools ?? []
        };

    private static UpdateProjectRequest MakeRequest(
        int id = 1,
        Status status = Status.InProgress,
        string name = "Test Project",
        int patternId = 1) =>
        new(id, name, status, null, null, null, patternId, []);

    private static (IProjectRepository repo, IWoolService woolService, IWoolUsageRepository usageRepo, ProjectService sut) MakeSut(Project? existing = null)
    {
        var repo = Substitute.For<IProjectRepository>();
        var woolService = Substitute.For<IWoolService>();
        var usageRepo = Substitute.For<IWoolUsageRepository>();

        var project = existing ?? MakeProject();
        repo.GetByIdAsync(Arg.Any<int>()).Returns(ResultT<Project>.Ok(project));
        repo.UpdateAsync(Arg.Any<UpdateProjectRequest>()).Returns(ResultT<Project>.Ok(project));
        woolService.AddStockAsync(Arg.Any<int>(), Arg.Any<double>(), Arg.Any<int?>()).Returns(Result.Ok());
        usageRepo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(ResultT<WoolUsage>.Ok(MakeWoolUsage()));
        usageRepo.UpdateStockAlreadyUsedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>()).Returns(Result.Ok());

        return (repo, woolService, usageRepo, new ProjectService(repo, woolService, usageRepo));
    }

    // ---------------------------------------------------------------------------
    // 1. GetByIdAsync failures
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ProjectNotFound_ReturnsFailureWithoutCallingUpdateRepo()
    {
        var (repo, woolService, usageRepo, sut) = MakeSut();
        repo.GetByIdAsync(Arg.Any<int>()).Returns(ResultT<Project>.NotFound("Le projet 1 est introuvable."));

        var result = await sut.UpdateAsync(MakeRequest());

        result.Failed.Should().BeTrue();
        await repo.DidNotReceive().UpdateAsync(Arg.Any<UpdateProjectRequest>());
    }

    [Fact]
    public async Task UpdateAsync_GetByIdReturnsNullValue_ReturnsFailure()
    {
        var (repo, _, _, sut) = MakeSut();
        repo.GetByIdAsync(Arg.Any<int>()).Returns(ResultT<Project>.Ok(null!));

        var result = await sut.UpdateAsync(MakeRequest());

        result.Failed.Should().BeTrue();
        await repo.DidNotReceive().UpdateAsync(Arg.Any<UpdateProjectRequest>());
    }

    // ---------------------------------------------------------------------------
    // 2. Status != Finished : CompleteProjectAsync ne doit pas etre appele
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_StatusRemainsInProgress_DoesNotCallCompleteProject()
    {
        var existing = MakeProject(status: Status.InProgress);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);

        await sut.UpdateAsync(MakeRequest(status: Status.InProgress));

        await usageRepo.DidNotReceive().GetUsageAsync(Arg.Any<int>(), Arg.Any<int>());
        await woolService.DidNotReceive().AddStockAsync(Arg.Any<int>(), Arg.Any<double>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task UpdateAsync_ProjectAlreadyFinished_DoesNotCallCompleteProjectAgain()
    {
        var existing = MakeProject(status: Status.Finished);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);

        await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        await usageRepo.DidNotReceive().GetUsageAsync(Arg.Any<int>(), Arg.Any<int>());
        await woolService.DidNotReceive().AddStockAsync(Arg.Any<int>(), Arg.Any<double>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task UpdateAsync_StatusChangesToPaused_DoesNotCallCompleteProject()
    {
        var existing = MakeProject(status: Status.InProgress);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);

        await sut.UpdateAsync(MakeRequest(status: Status.Paused));

        await usageRepo.DidNotReceive().GetUsageAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    // ---------------------------------------------------------------------------
    // 3. Transition vers Finished sans laines : passe directement au repo
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_NoWools_CallsRepoUpdateDirectly()
    {
        var existing = MakeProject(status: Status.InProgress, wools: []);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateAsync(Arg.Any<UpdateProjectRequest>());
        await woolService.DidNotReceive().AddStockAsync(Arg.Any<int>(), Arg.Any<double>(), Arg.Any<int?>());
    }

    // ---------------------------------------------------------------------------
    // 4. CompleteProjectAsync : remainingToDeduct <= 0 => skip
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_WoolFullyDeducted_SkipsWool()
    {
        var wool = MakeWool(stock: 5000);
        // StockUsed == StockAlreadyUsed => remainingToDeduct = 0 => skip
        var usage = MakeWoolUsage(wool, stockUsed: 1000, stockAlreadyUsed: 1000);
        var existing = MakeProject(status: Status.InProgress, wools: [usage]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(ResultT<WoolUsage>.Ok(usage));

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Succeeded.Should().BeTrue();
        await woolService.DidNotReceive().AddStockAsync(Arg.Any<int>(), Arg.Any<double>(), Arg.Any<int?>());
        await usageRepo.DidNotReceive().UpdateStockAlreadyUsedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>());
    }

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_StockAlreadyUsedExceedsStockUsed_SkipsWool()
    {
        var wool = MakeWool(stock: 5000);
        var usage = MakeWoolUsage(wool, stockUsed: 500, stockAlreadyUsed: 1000);
        var existing = MakeProject(status: Status.InProgress, wools: [usage]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(ResultT<WoolUsage>.Ok(usage));

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Succeeded.Should().BeTrue();
        await woolService.DidNotReceive().AddStockAsync(Arg.Any<int>(), Arg.Any<double>(), Arg.Any<int?>());
    }

    // ---------------------------------------------------------------------------
    // 5. CompleteProjectAsync : stock insuffisant
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_InsufficientStock_ReturnsFailure()
    {
        var wool = MakeWool(id: 1, name: "Merino", stock: 100);
        // remainingToDeduct = 1000 - 0 = 1000 > stock 100
        var usage = MakeWoolUsage(wool, stockUsed: 1000, stockAlreadyUsed: 0);
        var existing = MakeProject(status: Status.InProgress, wools: [usage]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(ResultT<WoolUsage>.Ok(usage));

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("Le stock disponible est insuffisant pour Merino.");
        await woolService.DidNotReceive().AddStockAsync(Arg.Any<int>(), Arg.Any<double>(), Arg.Any<int?>());
        await repo.DidNotReceive().UpdateAsync(Arg.Any<UpdateProjectRequest>());
    }

    // ---------------------------------------------------------------------------
    // 6. CompleteProjectAsync : appels corrects au repo
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_CallsAddStockWithNegativeRemainingToDeduct()
    {
        var wool = MakeWool(id: 1, stock: 5000);
        // remainingToDeduct = 2000 - 500 = 1500
        var usage = MakeWoolUsage(wool, stockUsed: 2000, stockAlreadyUsed: 500);
        var existing = MakeProject(status: Status.InProgress, wools: [usage]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(1, 1).Returns(ResultT<WoolUsage>.Ok(usage));

        await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        await woolService.Received(1).AddStockAsync(1, -1500, 1);
    }

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_CallsUpdateStockAlreadyUsedWithCorrectValue()
    {
        var wool = MakeWool(id: 1, stock: 5000);
        // remainingToDeduct = 2000 - 500 = 1500 => new StockAlreadyUsed = 500 + 1500 = 2000
        var usage = MakeWoolUsage(wool, stockUsed: 2000, stockAlreadyUsed: 500);
        var existing = MakeProject(status: Status.InProgress, wools: [usage]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(1, 1).Returns(ResultT<WoolUsage>.Ok(usage));

        await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        await usageRepo.Received(1).UpdateStockAlreadyUsedAsync(1, 1, 2000);
    }

    // ---------------------------------------------------------------------------
    // 7. CompleteProjectAsync : erreurs repo
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_GetUsageFails_ReturnsFailure()
    {
        var wool = MakeWool(stock: 5000);
        var usage = MakeWoolUsage(wool, stockUsed: 1000, stockAlreadyUsed: 0);
        var existing = MakeProject(status: Status.InProgress, wools: [usage]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(ResultT<WoolUsage>.Failure("db error"));

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("db error");
        await repo.DidNotReceive().UpdateAsync(Arg.Any<UpdateProjectRequest>());
    }

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_AddStockFails_ReturnsFailure()
    {
        var wool = MakeWool(id: 1, stock: 5000);
        var usage = MakeWoolUsage(wool, stockUsed: 1000, stockAlreadyUsed: 0);
        var existing = MakeProject(status: Status.InProgress, wools: [usage]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(ResultT<WoolUsage>.Ok(usage));
        woolService.AddStockAsync(Arg.Any<int>(), Arg.Any<double>(), Arg.Any<int?>()).Returns(Result.Failure("db error"));

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("db error");
        await repo.DidNotReceive().UpdateAsync(Arg.Any<UpdateProjectRequest>());
    }

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_UpdateStockAlreadyUsedFails_ReturnsFailure()
    {
        var wool = MakeWool(id: 1, stock: 5000);
        var usage = MakeWoolUsage(wool, stockUsed: 1000, stockAlreadyUsed: 0);
        var existing = MakeProject(status: Status.InProgress, wools: [usage]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>()).Returns(ResultT<WoolUsage>.Ok(usage));
        usageRepo.UpdateStockAlreadyUsedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>()).Returns(Result.Failure("db error"));

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("db error");
        await repo.DidNotReceive().UpdateAsync(Arg.Any<UpdateProjectRequest>());
    }

    // ---------------------------------------------------------------------------
    // 8. Plusieurs laines : iteration et arret au premier echec
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_MultipleWools_ProcessesAllWhenAllSucceed()
    {
        var wool1 = MakeWool(id: 1, stock: 5000);
        var wool2 = MakeWool(id: 2, stock: 5000);
        var usage1 = MakeWoolUsage(wool1, stockUsed: 1000, stockAlreadyUsed: 0);
        var usage2 = MakeWoolUsage(wool2, stockUsed: 2000, stockAlreadyUsed: 0);
        var existing = MakeProject(status: Status.InProgress, wools: [usage1, usage2]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(1, 1).Returns(ResultT<WoolUsage>.Ok(usage1));
        usageRepo.GetUsageAsync(1, 2).Returns(ResultT<WoolUsage>.Ok(usage2));

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Succeeded.Should().BeTrue();
        await woolService.Received(1).AddStockAsync(1, -1000, 1);
        await woolService.Received(1).AddStockAsync(2, -2000, 1);
        await usageRepo.Received(1).UpdateStockAlreadyUsedAsync(1, 1, 1000);
        await usageRepo.Received(1).UpdateStockAlreadyUsedAsync(1, 2, 2000);
    }

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_MultipleWools_StopsAtFirstFailure()
    {
        var wool1 = MakeWool(id: 1, name: "Merino", stock: 5000);
        var wool2 = MakeWool(id: 2, stock: 5000);
        var usage1 = MakeWoolUsage(wool1, stockUsed: 1000, stockAlreadyUsed: 0);
        var usage2 = MakeWoolUsage(wool2, stockUsed: 2000, stockAlreadyUsed: 0);
        var existing = MakeProject(status: Status.InProgress, wools: [usage1, usage2]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(1, 1).Returns(ResultT<WoolUsage>.Ok(usage1));
        usageRepo.GetUsageAsync(1, 2).Returns(ResultT<WoolUsage>.Ok(usage2));
        woolService.AddStockAsync(1, Arg.Any<double>(), Arg.Any<int?>()).Returns(Result.Failure("db error"));

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Failed.Should().BeTrue();
        await woolService.DidNotReceive().AddStockAsync(2, Arg.Any<double>(), Arg.Any<int?>());
    }

    // ---------------------------------------------------------------------------
    // 9. Happy path : repo.UpdateAsync est bien appele apres CompleteProjectAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_TransitionToFinished_AllSucceeds_CallsRepoUpdate()
    {
        var wool = MakeWool(id: 1, stock: 5000);
        var usage = MakeWoolUsage(wool, stockUsed: 1000, stockAlreadyUsed: 0);
        var existing = MakeProject(status: Status.InProgress, wools: [usage]);
        var (repo, woolService, usageRepo, sut) = MakeSut(existing);
        usageRepo.GetUsageAsync(1, 1).Returns(ResultT<WoolUsage>.Ok(usage));

        var result = await sut.UpdateAsync(MakeRequest(status: Status.Finished));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateAsync(Arg.Any<UpdateProjectRequest>());
    }

    [Fact]
    public async Task UpdateAsync_ForwardsCorrectRequestToRepo()
    {
        var existing = MakeProject(status: Status.InProgress);
        var (repo, _, _, sut) = MakeSut(existing);
        var request = MakeRequest(id: 42, status: Status.Paused, name: "Mon projet");

        await sut.UpdateAsync(request);

        await repo.Received(1).UpdateAsync(Arg.Is<UpdateProjectRequest>(r =>
            r.Id == 42 && r.Status == Status.Paused && r.Name == "Mon projet"));
    }
}
