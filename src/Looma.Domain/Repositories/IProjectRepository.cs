// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Request;

namespace Looma.Domain.Repositories;

public interface IProjectRepository
{
    /// <summary>
    /// Retrieves all projects from the database.
    /// </summary>
    /// <returns>A list of projects wrapped in a ResultT.</returns>
    Task<ResultT<IReadOnlyList<Project>>> GetAllAsync();

    /// <summary>
    /// Retrieves a project by its ID.
    /// </summary>
    /// <param name="id">The ID of the project to retrieve.</param>
    /// <returns>The project wrapped in a ResultT.</returns>
    Task<ResultT<Project>> GetByIdAsync(int id);

    /// <summary>
    /// Adds a new project to the database.
    /// </summary>
    /// <param name="request">The request containing the project's information.</param>
    /// <returns>The newly created project wrapped in a ResultT.</returns>
    Task<ResultT<Project>> AddAsync(CreateProjectRequest request);

    /// <summary>
    /// Updates an existing project in the database.
    /// </summary>
    /// <param name="request">The request containing the updated project's information.</param>
    /// <returns>The updated project wrapped in a ResultT.</returns>
    Task<ResultT<Project>> UpdateAsync(UpdateProjectRequest request);

    /// <summary>
    /// Deletes a project from the database.
    /// </summary>
    /// <param name="id">The ID of the project to delete.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> DeleteAsync(int id);
}
