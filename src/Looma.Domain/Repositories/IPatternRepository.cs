// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Request;

namespace Looma.Domain.Repositories;

public interface IPatternRepository
{
    /// <summary>
    /// Retrieves all patterns from the database.
    /// </summary>
    /// <returns>A list of patterns wrapped in a ResultT.</returns>
    Task<ResultT<IReadOnlyList<Pattern>>> GetAllAsync();

    /// <summary>
    /// Retrieves a pattern by its ID.
    /// </summary>
    /// <param name="id">The ID of the pattern to retrieve.</param>
    /// <returns>The pattern wrapped in a ResultT.</returns>
    Task<ResultT<Pattern>> GetByIdAsync(int id);

    /// <summary>
    /// Adds a new pattern to the database.
    /// </summary>
    /// <param name="request">The request containing the pattern's information.</param>
    /// <returns>The newly created pattern wrapped in a ResultT.</returns>
    Task<ResultT<Pattern>> AddAsync(CreatePatternRequest request);

    /// <summary>
    /// Updates an existing pattern in the database.
    /// </summary>
    /// <param name="request">The request containing the updated pattern's information.</param>
    /// <returns>The updated pattern wrapped in a ResultT.</returns>
    Task<ResultT<Pattern>> UpdateAsync(UpdatePatternRequest request);

    /// <summary>
    /// Adds a document to a pattern in the database.
    /// </summary>
    /// <param name="patternId">The ID of the pattern to add the document to.</param>
    /// <param name="documentId">The ID of the document to add.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> AddDocumentAsync(int patternId, Guid documentId);

    /// <summary>
    /// Removes a document from a pattern in the database.
    /// </summary>
    /// <param name="patternId">The ID of the pattern to remove the document from.</param>
    /// <param name="documentId">The ID of the document to remove.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> RemoveDocumentAsync(int patternId, Guid documentId);

    /// <summary>
    /// Deletes a pattern from the database.
    /// </summary>
    /// <param name="id">The ID of the pattern to delete.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> DeleteAsync(int id);
}
