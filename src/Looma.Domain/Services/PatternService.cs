// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;
using Looma.Domain.Request;

namespace Looma.Domain.Services;

public sealed class PatternService(IPatternRepository repository, IDomainLogger logger)
    : DomainServiceBase(logger), IPatternService
{
    public Task<ResultT<IReadOnlyList<Pattern>>> GetAllAsync() =>
        ExecuteAsync("Patterns.GetAll", repository.GetAllAsync);

    public Task<ResultT<Pattern>> GetByIdAsync(int id) =>
        ExecuteAsync($"Patterns.GetById({id})", () => repository.GetByIdAsync(id));

    public Task<ResultT<Pattern>> AddAsync(CreatePatternRequest request) =>
        ExecuteAsync("Patterns.Add", () => repository.AddAsync(request));

    public Task<ResultT<Pattern>> UpdateAsync(UpdatePatternRequest request) =>
        ExecuteAsync($"Patterns.Update({request.Id})", () => repository.UpdateAsync(request));

    public Task<Result> AddDocumentAsync(int patternId, Guid documentId) =>
        ExecuteAsync($"Patterns.AddDocument({patternId}, {documentId})", () => repository.AddDocumentAsync(patternId, documentId));

    public Task<Result> RemoveDocumentAsync(int patternId, Guid documentId) =>
        ExecuteAsync($"Patterns.RemoveDocument({patternId}, {documentId})", () => repository.RemoveDocumentAsync(patternId, documentId));

    public Task<Result> DeleteAsync(int id) =>
        ExecuteAsync($"Patterns.Delete({id})", () => repository.DeleteAsync(id));
}
