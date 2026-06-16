// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;
using Looma.Domain.Request;

namespace Looma.Domain.Services;

public sealed class DocumentService(IDocumentRepository repository, IDomainLogger logger)
    : DomainServiceBase(logger), IDocumentService
{
    public Task<ResultT<IReadOnlyList<Document>>> GetAllAsync() =>
        ExecuteAsync("Documents.GetAll", repository.GetAllAsync);

    public Task<ResultT<Document>> GetByIdAsync(Guid id) =>
        ExecuteAsync($"Documents.GetById({id})", () => repository.GetByIdAsync(id));

    public Task<ResultT<Document>> AddAsync(CreateDocumentRequest request) =>
        ExecuteAsync("Documents.Add", () => repository.AddAsync(request));

    public Task<ResultT<Document>> UpdateAsync(UpdateDocumentRequest request) =>
        ExecuteAsync($"Documents.Update({request.Id})", () => repository.UpdateAsync(request));

    public Task<Result> DeleteAsync(Guid id) =>
        ExecuteAsync($"Documents.Delete({id})", () => repository.DeleteAsync(id));

    public Task<Result> OpenAsync(Guid id) =>
        ExecuteAsync($"Documents.Open({id})", () => repository.OpenAsync(id));
}
