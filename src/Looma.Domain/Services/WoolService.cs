// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.IServices;
using Looma.Domain.Logging;
using Looma.Domain.Refresh;
using Looma.Domain.Repositories;
using Looma.Domain.Request;
using Looma.Domain.Localization;

namespace Looma.Domain.Services;

public sealed class WoolService(
    IWoolRepository repository,
    ITrackedWoolRepository trackedWoolRepository,
    IDomainLogger logger,
    IDataRefreshService? refreshService = null,
    IUnitOfWork? unitOfWork = null)
    : DomainServiceBase(logger), IWoolService
{
    public Task<ResultT<IReadOnlyList<Wool>>> GetAllAsync() =>
        ExecuteAsync("Wools.GetAll", repository.GetAllAsync);

    public Task<ResultT<Wool>> GetByIdAsync(int id) =>
        ExecuteAsync($"Wools.GetById({id})", () => repository.GetByIdAsync(id));

    public async Task<ResultT<Wool>> AddAsync(CreateWoolRequest request)
    {
        var validation = Validate(request);
        if (validation.Failed)
            return ResultT<Wool>.Failure(validation.Error!);

        var result = await ExecuteAsync("Wools.Add", () => repository.AddAsync(request));
        PublishIfSucceeded(result, RefreshScope.Wools, "Wool added.");
        return result;
    }

    public async Task<ResultT<Wool>> UpdateAsync(UpdateWoolRequest request)
    {
        var validation = Validate(request);
        if (validation.Failed)
            return ResultT<Wool>.Failure(validation.Error!);

        var result = await ExecuteAsync($"Wools.Update({request.Id})", () => repository.UpdateAsync(request));
        PublishIfSucceeded(result, RefreshScope.Wools, $"Wool {request.Id} updated.");
        return result;
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var result = await ExecuteAsync($"Wools.Delete({id})", () => repository.DeleteAsync(id));
        PublishIfSucceeded(result, RefreshScope.Wools, $"Wool {id} deleted.");
        return result;
    }

    public Task<Result> AddStockAsync(int id, double quantity) =>
        AddStockAsync(id, quantity, null);

    public async Task<Result> AddStockAsync(int id, double quantity, int? projectId)
    {
        if (!double.IsFinite(quantity))
            return Result.Failure(Localizer.Get("Data_Errors_InvalidStockQuantity"));

        var result = await ExecuteAsync($"Wools.AddStock({id}, {quantity})", () => unitOfWork is null
            ? ApplyStockChangeAsync(id, quantity, projectId)
            : unitOfWork.ExecuteAsync(() => ApplyStockChangeAsync(id, quantity, projectId)));

        PublishIfSucceeded(result, RefreshScope.Wools, $"Wool {id} stock changed.");
        return result;
    }

    private async Task<Result> ApplyStockChangeAsync(int id, double quantity, int? projectId)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing.Failed || existing.Value is null)
            return existing.Status == ResultStatus.NotFound
                ? Result.NotFound(existing.Error ?? Localizer.Format("Errors_WoolNotFound", id))
                : Result.Failure(existing.Error ?? Localizer.Format("Errors_UnableToLoadWoolNoDetail", id));

        var trackedQuantity = Math.Max(0, existing.Value.Stock + quantity) - existing.Value.Stock;

        var stockResult = await repository.AddStock(id, quantity);
        if (stockResult.Failed)
            return stockResult;

        if (trackedQuantity != 0)
        {
            var trackingResult = await trackedWoolRepository.AddAsync(id, trackedQuantity, projectId);
            if (trackingResult.Failed)
                return trackingResult;
        }

        return Result.Ok();
    }

    private void PublishIfSucceeded(ResultBase result, RefreshScope scope, string reason)
    {
        if (result.Succeeded)
            refreshService?.RequestRefresh(scope, reason);
    }

    private static Result Validate(CreateWoolRequest request)
    {
        if (!double.IsFinite(request.Stock) || request.Stock < 0)
            return Result.Failure(Localizer.Get("Data_Errors_StockMustBePositive"));

        return ValidateWool(
            request.Name,
            request.Brand,
            request.Material,
            request.Weight,
            request.Length,
            request.NeedleMinSize,
            request.NeedleMaxSize);
    }

    private static Result Validate(UpdateWoolRequest request) =>
        ValidateWool(
            request.Name,
            request.Brand,
            request.Material,
            request.Weight,
            request.Length,
            request.NeedleMinSize,
            request.NeedleMaxSize);

    private static Result ValidateWool(
        string name,
        string brand,
        string material,
        double weight,
        double length,
        double needleMinSize,
        double needleMaxSize)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(Localizer.Get("Errors_WoolNameRequired"));

        if (string.IsNullOrWhiteSpace(brand))
            return Result.Failure(Localizer.Get("Errors_WoolBrandRequired"));

        if (string.IsNullOrWhiteSpace(material))
            return Result.Failure(Localizer.Get("Errors_WoolMaterialRequired"));

        if (!double.IsFinite(weight) || weight <= 0)
            return Result.Failure(Localizer.Get("Errors_WoolWeightPositive"));

        if (!double.IsFinite(length) || length <= 0)
            return Result.Failure(Localizer.Get("Errors_WoolLengthPositive"));

        if (Wool.FindNeedleRange(needleMinSize, needleMaxSize) is null)
            return Result.Failure(Localizer.Get("Errors_WoolNeedleRangeUnknown"));

        return Result.Ok();
    }
}
