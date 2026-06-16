// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using FluentAssertions;
using Looma.Domain.Core;
using Looma.Domain.Logging;
using Looma.Domain.Refresh;
using Looma.Domain.Repositories;
using Looma.Domain.Services;
using NSubstitute;

namespace Looma.Domain.Tests.Services;

public sealed class PatternServiceTests
{
    [Fact]
    public async Task RemoveDocumentAsync_notifies_patterns_and_documents_refresh()
    {
        var repository = Substitute.For<IPatternRepository>();
        var refreshService = Substitute.For<IDataRefreshService>();
        var sut = new PatternService(repository, Substitute.For<IDomainLogger>(), refreshService);
        var documentId = Guid.NewGuid();
        repository.RemoveDocumentAsync(12, documentId).Returns(Result.Ok());

        var result = await sut.RemoveDocumentAsync(12, documentId);

        result.Succeeded.Should().BeTrue(result.Error);
        refreshService.Received(1).RequestRefresh(
            RefreshScope.Patterns | RefreshScope.Documents,
            $"Document {documentId} removed from pattern 12.");
    }

    [Fact]
    public async Task DeleteAsync_notifies_documents_refresh_because_attached_documents_are_deleted()
    {
        var repository = Substitute.For<IPatternRepository>();
        var refreshService = Substitute.For<IDataRefreshService>();
        var sut = new PatternService(repository, Substitute.For<IDomainLogger>(), refreshService);
        repository.DeleteAsync(12).Returns(Result.Ok());

        var result = await sut.DeleteAsync(12);

        result.Succeeded.Should().BeTrue(result.Error);
        refreshService.Received(1).RequestRefresh(
            RefreshScope.Patterns | RefreshScope.Projects | RefreshScope.Documents,
            "Pattern 12 deleted.");
    }
}
