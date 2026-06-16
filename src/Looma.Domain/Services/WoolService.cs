// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;
using Looma.Domain.Request;

namespace Looma.Domain.Services;

public sealed class WoolService(IWoolRepository repository, IDomainLogger logger)
    : DomainServiceBase(logger), IWoolService
{
    public Task<ResultT<IReadOnlyList<Wool>>> GetAllAsync() =>
        ExecuteAsync("Wools.GetAll", repository.GetAllAsync);

    public Task<ResultT<Wool>> GetByIdAsync(int id) =>
        ExecuteAsync($"Wools.GetById({id})", () => repository.GetByIdAsync(id));

    public Task<ResultT<Wool>> AddAsync(CreateWoolRequest request) =>
        ExecuteAsync("Wools.Add", () => repository.AddAsync(request));

    public Task<ResultT<Wool>> UpdateAsync(UpdateWoolRequest request) =>
        ExecuteAsync($"Wools.Update({request.Id})", () => repository.UpdateAsync(request));

    public Task<Result> DeleteAsync(int id) =>
        ExecuteAsync($"Wools.Delete({id})", () => repository.DeleteAsync(id));

    public Task<Result> AddStockAsync(int id, double quantity) =>
        ExecuteAsync($"Wools.AddStock({id}, {quantity})", () => repository.AddStock(id, quantity));
}
