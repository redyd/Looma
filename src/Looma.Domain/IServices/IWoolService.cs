// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Request;

namespace Looma.Domain.IServices;

public interface IWoolService
{
    Task<ResultT<IReadOnlyList<Wool>>> GetAllAsync();
    Task<ResultT<Wool>> GetByIdAsync(int id);
    Task<ResultT<Wool>> AddAsync(CreateWoolRequest request);
    Task<ResultT<Wool>> UpdateAsync(UpdateWoolRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> AddStockAsync(int id, double quantity);
}
