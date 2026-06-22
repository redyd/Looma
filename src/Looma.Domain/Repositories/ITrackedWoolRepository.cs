// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;

namespace Looma.Domain.Repositories;

public interface ITrackedWoolRepository
{
    Task<Result> AddAsync(int woolId, double quantity, int? projectId = null, DateTime? date = null);
    Task<ResultT<IReadOnlyList<TrackedWoolMovement>>> GetMovementsAsync(DateTime? from = null);
}
