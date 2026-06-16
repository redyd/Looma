// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Entities;
using Looma.Domain.Core;
using Looma.Domain.Request;

namespace Looma.Domain.Repositories;

public interface IWoolRepository
{
    /// <summary>
    /// Retrieves all wool from the database.
    /// </summary>
    /// <returns>A list of wool wrapped in a ResultT.</returns>
    Task<ResultT<IReadOnlyList<Wool>>> GetAllAsync();

    /// <summary>
    /// Retrieves a wool by its ID.
    /// </summary>
    /// <param name="id">The ID of the wool to retrieve.</param>
    /// <returns>The wool wrapped in a ResultT.</returns>
    Task<ResultT<Wool>> GetByIdAsync(int id);

    /// <summary>
    /// Adds a new wool to the database.
    /// </summary>
    /// <param name="request">The request containing the wool's information.</param>
    /// <returns>The newly created wool wrapped in a ResultT.</returns>
    Task<ResultT<Wool>> AddAsync(CreateWoolRequest request);

    /// <summary>
    /// Updates an existing wool in the database.
    /// </summary>
    /// <param name="request">The request containing the updated wool's information.</param>
    /// <returns>The updated wool wrapped in a ResultT.</returns>
    Task<ResultT<Wool>> UpdateAsync(UpdateWoolRequest request);

    /// <summary>
    /// Deletes a wool from the database.
    /// </summary>
    /// <param name="id">The ID of the wool to delete.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> DeleteAsync(int id);

    /// <summary>
    /// Adds stock to a wool in the database.
    /// </summary>
    /// <param name="id">The ID of the wool to update.</param>
    /// <param name="quantity">The quantity of stock to add.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> AddStock(int id, double quantity);
}
