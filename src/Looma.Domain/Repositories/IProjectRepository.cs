// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Request;

namespace Looma.Domain.Repositories;

public interface IProjectRepository
{
    Task<ResultT<IReadOnlyList<Project>>> GetAllAsync();
    Task<ResultT<Project>> GetByIdAsync(int id);
    Task<ResultT<Project>> AddAsync(CreateProjectRequest request);
    Task<ResultT<Project>> UpdateAsync(UpdateProjectRequest request);
    Task<ResultT<Project>> UpdateWoolUsageAsync(int projectId, int woolId, double stockUsed);
    Task<Result> DeleteAsync(int id);
}
