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

public sealed class PatternService(IPatternRepository repository, IDomainLogger logger, IDataRefreshService? refreshService = null)
    : DomainServiceBase(logger), IPatternService
{
    public Task<ResultT<IReadOnlyList<Pattern>>> GetAllAsync() =>
        ExecuteAsync("Patterns.GetAll", repository.GetAllAsync);

    public Task<ResultT<Pattern>> GetByIdAsync(int id) =>
        ExecuteAsync($"Patterns.GetById({id})", () => repository.GetByIdAsync(id));

    public async Task<ResultT<Pattern>> AddAsync(CreatePatternRequest request)
    {
        var result = await ExecuteAsync("Patterns.Add", () => repository.AddAsync(request));
        PublishIfSucceeded(result, RefreshScope.Patterns, "Pattern added.");
        return result;
    }

    public async Task<ResultT<Pattern>> UpdateAsync(UpdatePatternRequest request)
    {
        var result = await ExecuteAsync($"Patterns.Update({request.Id})", () => repository.UpdateAsync(request));
        PublishIfSucceeded(result, RefreshScope.Patterns | RefreshScope.Projects, $"Pattern {request.Id} updated.");
        return result;
    }

    public async Task<Result> AddDocumentAsync(int patternId, Guid documentId)
    {
        var result = await ExecuteAsync($"Patterns.AddDocument({patternId}, {documentId})", () => repository.AddDocumentAsync(patternId, documentId));
        PublishIfSucceeded(result, RefreshScope.Patterns | RefreshScope.Documents, $"Document {documentId} added to pattern {patternId}.");
        return result;
    }

    public async Task<Result> RemoveDocumentAsync(int patternId, Guid documentId)
    {
        var result = await ExecuteAsync($"Patterns.RemoveDocument({patternId}, {documentId})", () => repository.RemoveDocumentAsync(patternId, documentId));
        PublishIfSucceeded(result, RefreshScope.Patterns | RefreshScope.Documents, $"Document {documentId} removed from pattern {patternId}.");
        return result;
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var result = await ExecuteAsync($"Patterns.Delete({id})", () => repository.DeleteAsync(id));
        PublishIfSucceeded(result, RefreshScope.Patterns | RefreshScope.Projects | RefreshScope.Documents, $"Pattern {id} deleted.");
        return result;
    }

    private void PublishIfSucceeded(ResultBase result, RefreshScope scope, string reason)
    {
        if (result.Succeeded)
            refreshService?.RequestRefresh(scope, reason);
    }
}
