// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Request;

namespace Looma.Domain.Repositories;

public interface IPatternRepository
{
    Task<ResultT<IReadOnlyList<Pattern>>> GetAllAsync();
    Task<ResultT<Pattern>> GetByIdAsync(int id);
    Task<ResultT<Pattern>> AddAsync(CreatePatternRequest request);
    Task<ResultT<Pattern>> UpdateAsync(UpdatePatternRequest request);
    Task<Result> AddDocumentAsync(int patternId, Guid documentId);
    Task<Result> RemoveDocumentAsync(int patternId, Guid documentId);
    Task<Result> DeleteAsync(int id);
}
