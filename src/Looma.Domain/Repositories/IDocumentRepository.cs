// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Request;

namespace Looma.Domain.Repositories;

public interface IDocumentRepository
{
    Task<ResultT<IReadOnlyList<Document>>> GetAllAsync();
    Task<ResultT<Document>> GetByIdAsync(Guid id);
    Task<ResultT<Document>> AddAsync(CreateDocumentRequest request);
    Task<ResultT<Document>> UpdateAsync(UpdateDocumentRequest request);
    Task<Result> DeleteAsync(Guid id);
    Task<Result> OpenAsync(Guid id);
}
