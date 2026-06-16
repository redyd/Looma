// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Request;

namespace Looma.Domain.Repositories;

public interface IDocumentRepository
{
    /// <summary>
    /// Retrieves all documents from the database, including their pattern and project associations.
    /// </summary>
    /// <returns>A list of documents wrapped in a ResultT.</returns>
    Task<ResultT<IReadOnlyList<Document>>> GetAllAsync();

    /// <summary>
    /// Retrieves a document by its ID, including its pattern and project associations.
    /// </summary>
    /// <param name="id">The ID of the document to retrieve.</param>
    /// <returns>The document wrapped in a ResultT.</returns>
    Task<ResultT<Document>> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds a new document to the database.
    /// </summary>
    /// <param name="request">The request containing the document's source name and nickname.</param>
    /// <returns>The newly created document wrapped in a ResultT.</returns>
    Task<ResultT<Document>> AddAsync(CreateDocumentRequest request);

    /// <summary>
    /// Updates an existing document in the database.
    /// </summary>
    /// <param name="request">The request containing the updated document's information.</param>
    /// <returns>The updated document wrapped in a ResultT.</returns>
    Task<ResultT<Document>> UpdateAsync(UpdateDocumentRequest request);

    /// <summary>
    /// Deletes a document from the database.
    /// </summary>
    /// <param name="id">The ID of the document to delete.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> DeleteAsync(Guid id);

    /// <summary>
    /// Opens a document from the database.
    /// </summary>
    /// <param name="id">The ID of the document to open.</param>
    /// <returns>A Result indicating the success or failure of the operation.</returns>
    Task<Result> OpenAsync(Guid id);
}
