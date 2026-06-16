// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Logging;
using Looma.Domain.Refresh;
using Looma.Domain.Repositories;
using Looma.Domain.Request;

namespace Looma.Domain.Services;

public sealed class DocumentService(IDocumentRepository repository, IDomainLogger logger, IDataRefreshService? refreshService = null)
    : DomainServiceBase(logger), IDocumentService
{
    public Task<ResultT<IReadOnlyList<Document>>> GetAllAsync() =>
        ExecuteAsync("Documents.GetAll", repository.GetAllAsync);

    public Task<ResultT<Document>> GetByIdAsync(Guid id) =>
        ExecuteAsync($"Documents.GetById({id})", () => repository.GetByIdAsync(id));

    public async Task<ResultT<Document>> AddAsync(CreateDocumentRequest request)
    {
        var result = await ExecuteAsync("Documents.Add", () => repository.AddAsync(request));
        PublishIfSucceeded(result, GetDocumentRefreshScope(request.PatternId, request.ProjectId), "Document added.");
        return result;
    }

    public async Task<ResultT<IReadOnlyList<Document>>> AddAllAsync(IReadOnlyList<CreateDocumentRequest> requests)
    {
        var result = await ExecuteAsync("Documents.AddAll", async () =>
        {
            var documents = new List<Document>(requests.Count);

            foreach (var request in requests)
            {
                var documentResult = await repository.AddAsync(request);
                if (documentResult.Failed || documentResult.Value is null)
                {
                    return ResultT<IReadOnlyList<Document>>.Failure(
                        documentResult.Error ?? "Impossible d'ajouter les documents.");
                }

                documents.Add(documentResult.Value);
            }

            return ResultT<IReadOnlyList<Document>>.Ok(documents);
        });

        if (requests.Count > 0)
        {
            var scope = requests
                .Select(request => GetDocumentRefreshScope(request.PatternId, request.ProjectId))
                .Aggregate(RefreshScope.None, (current, next) => current | next);

            PublishIfSucceeded(result, scope, $"{requests.Count} documents added.");
        }

        return result;
    }

    public async Task<ResultT<Document>> UpdateAsync(UpdateDocumentRequest request)
    {
        var result = await ExecuteAsync($"Documents.Update({request.Id})", () => repository.UpdateAsync(request));
        PublishIfSucceeded(result, RefreshScope.Documents | RefreshScope.Patterns | RefreshScope.Projects, $"Document {request.Id} updated.");
        return result;
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var result = await ExecuteAsync($"Documents.Delete({id})", () => repository.DeleteAsync(id));
        PublishIfSucceeded(result, RefreshScope.Documents | RefreshScope.Patterns | RefreshScope.Projects, $"Document {id} deleted.");
        return result;
    }

    public Task<Result> OpenAsync(Guid id) =>
        ExecuteAsync($"Documents.Open({id})", () => repository.OpenAsync(id));

    private static RefreshScope GetDocumentRefreshScope(int? patternId, int? projectId)
    {
        var scope = RefreshScope.Documents;
        if (patternId.HasValue)
            scope |= RefreshScope.Patterns;
        if (projectId.HasValue)
            scope |= RefreshScope.Projects;
        return scope;
    }

    private void PublishIfSucceeded(ResultBase result, RefreshScope scope, string reason)
    {
        if (result.Succeeded)
            refreshService?.RequestRefresh(scope, reason);
    }
}
