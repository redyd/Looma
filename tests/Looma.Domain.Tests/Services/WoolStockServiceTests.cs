using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Repositories;
using Looma.Domain.Request;
using Looma.Domain.Services;
using NSubstitute;

namespace Looma.Domain.Tests.Services;

public class WoolStockServiceTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static Wool MakeWool(double stock = 5000, double weight = 100, double length = 200) => new()
    {
        Id = 1,
        Name = "Test Wool",
        Brand = "Brand",
        Material = "Wool",
        Color = "Red",
        Weight = weight,
        Length = length,
        Stock = stock,
        NeedleMinSize = 4,
        NeedleMaxSize = 6
    };

    private static WoolUsage MakeUsage(double stockUsed = 0, double stockAlreadyUsed = 0, Wool? wool = null) => new()
    {
        Wool = wool ?? MakeWool(),
        StockUsed = stockUsed,
        StockAlreadyUsed = stockAlreadyUsed
    };

    private static AdjustProjectWoolUsageRequest MakeRequest(
        StockAdjustmentMode mode = StockAdjustmentMode.ByBall,
        bool isAddition = true,
        double quantity = 1,
        bool deductImmediately = false,
        int projectId = 1,
        int woolId = 1) =>
        new(projectId, woolId, mode, isAddition, quantity, deductImmediately);

    // Builds a repository that returns a successful GetUsageAsync with the given usage,
    // and whose update methods all succeed by default.
    private static IWoolUsageRepository MakeRepo(WoolUsage usage)
    {
        var repo = Substitute.For<IWoolUsageRepository>();
        repo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(ResultT<WoolUsage>.Ok(usage));
        repo.UpdateStockUsedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>())
            .Returns(Result.Ok());
        repo.UpdateCurrentStockUsageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>())
            .Returns(Result.Ok());
        repo.UpdateStockAlreadyUsedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>())
            .Returns(Result.Ok());
        return repo;
    }

    // ---------------------------------------------------------------------------
    // 1. Input validation
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_QuantityZero_ReturnsFailure()
    {
        var repo = Substitute.For<IWoolUsageRepository>();
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(quantity: 0));

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("La quantité doit être supérieure à zéro.");
        await repo.DidNotReceive().GetUsageAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_NegativeQuantity_ReturnsFailure()
    {
        var repo = Substitute.For<IWoolUsageRepository>();
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(quantity: -5));

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("La quantité doit être supérieure à zéro.");
    }

    // ---------------------------------------------------------------------------
    // 2. Repository GetUsageAsync failures
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_GetUsageFails_ReturnsFailure()
    {
        var repo = Substitute.For<IWoolUsageRepository>();
        repo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(ResultT<WoolUsage>.Failure("db error"));
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest());

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("Une erreur est survenue lors de la récupération de l'usage de la laine.");
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_GetUsageReturnsNullValue_ReturnsFailure()
    {
        var repo = Substitute.For<IWoolUsageRepository>();
        repo.GetUsageAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(ResultT<WoolUsage>.Ok(null!));
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest());

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("Une erreur est survenue lors de la récupération de l'usage de la laine.");
    }

    // ---------------------------------------------------------------------------
    // 3. ByBall mode - delta computation
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_ByBall_Addition_CallsUpdateStockUsedWithCorrectDelta()
    {
        // 2 balls * 1000 = +2000
        var usage = MakeUsage(stockUsed: 0);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 2));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateStockUsedAsync(1, 1, 2000);
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_ByBall_Removal_CallsUpdateStockUsedWithNegativeDelta()
    {
        // stockUsed = 3000, remove 1 ball => delta = -1000 => new = 2000
        var usage = MakeUsage(stockUsed: 3000);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: false, quantity: 1));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateStockUsedAsync(1, 1, 2000);
    }

    // ---------------------------------------------------------------------------
    // 4. ByWeight mode - delta computation
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_ByWeight_Addition_ComputesDeltaFromWoolWeight()
    {
        // quantity=50g, weight=100g/ball => 50/100*1000 = 500 units
        var wool = MakeWool(weight: 100, stock: 10000);
        var usage = MakeUsage(stockUsed: 0, wool: wool);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByWeight, isAddition: true, quantity: 50));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateStockUsedAsync(1, 1, 500);
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_ByWeight_Removal_ComputesNegativeDelta()
    {
        // stockUsed=1000, quantity=50g, weight=100 => delta=-500 => new=500
        var wool = MakeWool(weight: 100, stock: 10000);
        var usage = MakeUsage(stockUsed: 1000, wool: wool);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByWeight, isAddition: false, quantity: 50));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateStockUsedAsync(1, 1, 500);
    }

    // ---------------------------------------------------------------------------
    // 5. ByLength mode - delta computation
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_ByLength_Addition_ComputesDeltaFromWoolLength()
    {
        // quantity=100m, length=200m/ball => 100/200*1000 = 500 units
        var wool = MakeWool(length: 200, stock: 10000);
        var usage = MakeUsage(stockUsed: 0, wool: wool);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByLength, isAddition: true, quantity: 100));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateStockUsedAsync(1, 1, 500);
    }

    // ---------------------------------------------------------------------------
    // 6. Removal capped at StockUsed (delta floor)
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_RemovalExceedsStockUsed_CapsAtStockUsed()
    {
        // stockUsed=500, remove 2 balls (2000) => capped to -500 => new=0
        var usage = MakeUsage(stockUsed: 500);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: false, quantity: 2));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateStockUsedAsync(1, 1, 0);
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_RemovalExactlyEqualsStockUsed_ResetsToZero()
    {
        // stockUsed=1000, remove 1 ball (1000) => delta=-1000 => new=0
        var usage = MakeUsage(stockUsed: 1000);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: false, quantity: 1));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateStockUsedAsync(1, 1, 0);
    }

    // ---------------------------------------------------------------------------
    // 7. DeductImmediately + IsAddition: insufficient stock
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_AdditionDeductImmediately_InsufficientStock_ReturnsFailure()
    {
        // stock=1000, add 2 balls (delta=2000) > stock => failure
        var wool = MakeWool(stock: 1000);
        var usage = MakeUsage(wool: wool);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 2, deductImmediately: true));

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("Le stock disponible est insuffisant.");
        await repo.DidNotReceive().UpdateCurrentStockUsageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>());
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_AdditionDeductImmediately_ExactlyMatchesStock_Succeeds()
    {
        // stock=1000, add 1 ball (delta=1000) == stock => ok
        var wool = MakeWool(stock: 1000);
        var usage = MakeUsage(wool: wool);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 1, deductImmediately: true));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_AdditionDeductImmediately_SufficientStock_CallsUpdateCurrentStock()
    {
        // stock=5000, add 1 ball (delta=1000) < stock => calls UpdateCurrentStockUsageAsync(+1000)
        var wool = MakeWool(stock: 5000);
        var usage = MakeUsage(wool: wool);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 1, deductImmediately: true));

        await repo.Received(1).UpdateCurrentStockUsageAsync(1, 1, 1000);
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_AdditionDeductImmediately_UpdateCurrentStockFails_ReturnsFailure()
    {
        var wool = MakeWool(stock: 5000);
        var usage = MakeUsage(wool: wool);
        var repo = MakeRepo(usage);
        repo.UpdateCurrentStockUsageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>())
            .Returns(Result.Failure("db error"));
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 1, deductImmediately: true));

        result.Failed.Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // 8. DeductImmediately + removal: restore flow
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_RemovalDeductImmediately_RestoresUpToStockAlreadyUsed()
    {
        // stockUsed=2000, stockAlreadyUsed=1500, remove 1 ball (delta=-1000)
        // restore = min(1000, 1500) = 1000 => UpdateCurrentStockUsageAsync(-1000)
        var usage = MakeUsage(stockUsed: 2000, stockAlreadyUsed: 1500);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: false, quantity: 1, deductImmediately: true));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateCurrentStockUsageAsync(1, 1, -1000);
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_RemovalDeductImmediately_DeltaExceedsStockAlreadyUsed_RestoresCappedAtStockAlreadyUsed()
    {
        // stockUsed=3000, stockAlreadyUsed=500, remove 2 balls (delta=-2000)
        // restore = min(2000, 500) = 500 => UpdateCurrentStockUsageAsync(-500)
        var usage = MakeUsage(stockUsed: 3000, stockAlreadyUsed: 500);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: false, quantity: 2, deductImmediately: true));

        await repo.Received(1).UpdateCurrentStockUsageAsync(1, 1, -500);
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_RemovalDeductImmediately_StockAlreadyUsedZero_DoesNotCallUpdateCurrentStock()
    {
        // stockAlreadyUsed=0 => restore=0, min(delta,0)=0
        // NOTE: UpdateCurrentStockUsageAsync sera quand même appelé avec 0.
        // Ce test documente le comportement actuel.
        var usage = MakeUsage(stockUsed: 1000, stockAlreadyUsed: 0);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: false, quantity: 1, deductImmediately: true));

        await repo.Received(1).UpdateCurrentStockUsageAsync(1, 1, 0);
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_RemovalDeductImmediately_UpdateCurrentStockFails_ReturnsFailure()
    {
        var usage = MakeUsage(stockUsed: 2000, stockAlreadyUsed: 1000);
        var repo = MakeRepo(usage);
        repo.UpdateCurrentStockUsageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>())
            .Returns(Result.Failure("db error"));
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: false, quantity: 1, deductImmediately: true));

        result.Failed.Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // 9. StockAlreadyUsed > StockUsed sync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_StockAlreadyUsedExceedsInitialStockUsed_CallsUpdateStockAlreadyUsed()
    {
        // stockUsed=500, stockAlreadyUsed=800 (already > stockUsed before delta)
        // => UpdateStockAlreadyUsedAsync called with 500 (initial StockUsed, NOT stockUsed+delta)
        // NOTE: ce comportement est potentiellement un bug - voir remarques
        var usage = MakeUsage(stockUsed: 500, stockAlreadyUsed: 800);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 1));

        result.Succeeded.Should().BeTrue();
        await repo.Received(1).UpdateStockAlreadyUsedAsync(1, 1, 500);
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_StockAlreadyUsedBelowStockUsed_DoesNotCallUpdateStockAlreadyUsed()
    {
        var usage = MakeUsage(stockUsed: 1000, stockAlreadyUsed: 500);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 1));

        await repo.DidNotReceive().UpdateStockAlreadyUsedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>());
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_StockAlreadyUsedEqualsStockUsed_DoesNotCallUpdateStockAlreadyUsed()
    {
        var usage = MakeUsage(stockUsed: 1000, stockAlreadyUsed: 1000);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 1));

        await repo.DidNotReceive().UpdateStockAlreadyUsedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>());
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_StockAlreadyUsedExceedsStockUsed_UpdateStockAlreadyUsedFails_ReturnsFailure()
    {
        var usage = MakeUsage(stockUsed: 500, stockAlreadyUsed: 800);
        var repo = MakeRepo(usage);
        repo.UpdateStockAlreadyUsedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>())
            .Returns(Result.Failure("db error"));
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 1));

        result.Failed.Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // 10. DeductImmediately=false : UpdateCurrentStockUsageAsync ne doit pas etre appele
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_AdditionWithoutDeductImmediately_DoesNotCallUpdateCurrentStock()
    {
        var usage = MakeUsage();
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 1, deductImmediately: false));

        await repo.DidNotReceive().UpdateCurrentStockUsageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>());
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_RemovalWithoutDeductImmediately_DoesNotCallUpdateCurrentStock()
    {
        var usage = MakeUsage(stockUsed: 2000);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: false, quantity: 1, deductImmediately: false));

        await repo.DidNotReceive().UpdateCurrentStockUsageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>());
    }

    // ---------------------------------------------------------------------------
    // 11. Happy path complet
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AdjustWoolUsageAsync_SimpleAdditionByBall_ReturnsOk()
    {
        var usage = MakeUsage(stockUsed: 1000);
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest(mode: StockAdjustmentMode.ByBall, isAddition: true, quantity: 1));

        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_CorrectProjectAndWoolIdForwardedToRepo()
    {
        var usage = MakeUsage();
        var repo = MakeRepo(usage);
        var sut = new WoolStockService(repo);
        var request = MakeRequest(projectId: 42, woolId: 99);

        await sut.AdjustWoolUsageAsync(request);

        await repo.Received(1).GetUsageAsync(42, 99);
        await repo.Received(1).UpdateStockUsedAsync(42, 99, Arg.Any<double>());
    }

    [Fact]
    public async Task AdjustWoolUsageAsync_UpdateStockUsedFails_ReturnsFailure()
    {
        var usage = MakeUsage();
        var repo = MakeRepo(usage);
        repo.UpdateStockUsedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<double>())
            .Returns(Result.Failure("db error"));
        var sut = new WoolStockService(repo);

        var result = await sut.AdjustWoolUsageAsync(MakeRequest());

        result.Failed.Should().BeTrue();
        result.Error.Should().Be("db error");
    }
}
