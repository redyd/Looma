// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;

namespace Looma.Domain.Repositories;

public interface IWoolUsageRepository
{
    /// <summary>
    /// Retrieves all usages of wool in a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A list of wool usages wrapped in a ResultT.</returns>
    Task<ResultT<IReadOnlyList<WoolUsage>>> GetAllUsagesAsync(int projectId);

    /// <summary>
    /// Retrieves the usage of a wool in a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="woolId">The ID of the wool.</param>
    /// <returns>The wool usage wrapped in a ResultT.</returns>
    Task<ResultT<WoolUsage>> GetUsageAsync(int projectId, int woolId);

    /// <summary>
    /// Updates the current stock usage of a wool in a project. Update the wool's stock and stock already used.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="woolId">The ID of the wool.</param>
    /// <param name="stockUsage">The current stock usage.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> UpdateCurrentStockUsageAsync(int projectId, int woolId, double stockUsage);

    /// <summary>
    /// Updates the stock used of a wool in a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="woolId">The ID of the wool.</param>
    /// <param name="stockUsed">The amount of stock used.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> UpdateStockUsedAsync(int projectId, int woolId, double stockUsed);

    /// <summary>
    /// Updates the stock already used of a wool in a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="woolId">The ID of the wool.</param>
    /// <param name="stockAlreadyUsed">The amount of stock already used.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> UpdateStockAlreadyUsedAsync(int projectId, int woolId, double stockAlreadyUsed);
}
