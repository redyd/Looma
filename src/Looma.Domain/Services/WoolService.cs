// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Logging;
using Looma.Domain.Refresh;
using Looma.Domain.Repositories;
using Looma.Domain.Request;

namespace Looma.Domain.Services;

public sealed class WoolService(IWoolRepository repository, IDomainLogger logger, IDataRefreshService? refreshService = null)
    : DomainServiceBase(logger), IWoolService
{
    public Task<ResultT<IReadOnlyList<Wool>>> GetAllAsync() =>
        ExecuteAsync("Wools.GetAll", repository.GetAllAsync);

    public Task<ResultT<Wool>> GetByIdAsync(int id) =>
        ExecuteAsync($"Wools.GetById({id})", () => repository.GetByIdAsync(id));

    public async Task<ResultT<Wool>> AddAsync(CreateWoolRequest request)
    {
        var result = await ExecuteAsync("Wools.Add", () => repository.AddAsync(request));
        PublishIfSucceeded(result, RefreshScope.Wools, "Wool added.");
        return result;
    }

    public async Task<ResultT<Wool>> UpdateAsync(UpdateWoolRequest request)
    {
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

    public async Task<Result> AddStockAsync(int id, double quantity)
    {
        var result = await ExecuteAsync($"Wools.AddStock({id}, {quantity})", () => repository.AddStock(id, quantity));
        PublishIfSucceeded(result, RefreshScope.Wools, $"Wool {id} stock changed.");
        return result;
    }

    private void PublishIfSucceeded(ResultBase result, RefreshScope scope, string reason)
    {
        if (result.Succeeded)
            refreshService?.RequestRefresh(scope, reason);
    }
}
