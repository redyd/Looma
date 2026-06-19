// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;
using Looma.Domain.Services;
using FluentAssertions;
using NSubstitute;

namespace Looma.Domain.Tests.Services;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task GetReleaseNotesAsync_Logs_And_Returns_Repository_Value()
    {
        var repository = Substitute.For<ISettingsRepository>();
        var logger = Substitute.For<IDomainLogger>();
        repository.GetReleaseNotesAsync("1.2.3").Returns("## Notes");
        var sut = new SettingsService(repository, logger);

        var result = await sut.GetReleaseNotesAsync("1.2.3");

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be("## Notes");
        logger.Received(1).Log(DomainLogLevel.Information, "Settings.GetReleaseNotes(1.2.3) started.");
        logger.Received(1).Log(DomainLogLevel.Information, "Settings.GetReleaseNotes(1.2.3) completed.");
    }

    [Fact]
    public async Task SetReleaseNotesShownAsync_Logs_And_Forwards_To_Repository()
    {
        var repository = Substitute.For<ISettingsRepository>();
        var logger = Substitute.For<IDomainLogger>();
        var sut = new SettingsService(repository, logger);

        var result = await sut.SetReleaseNotesShownAsync("1.2.3", true);

        result.Succeeded.Should().BeTrue();
        await repository.Received(1).SetReleaseNotesShownAsync("1.2.3", true);
        logger.Received(1).Log(DomainLogLevel.Information, "Settings.SetReleaseNotesShown(1.2.3, True) started.");
        logger.Received(1).Log(DomainLogLevel.Information, "Settings.SetReleaseNotesShown(1.2.3, True) completed.");
    }

    [Fact]
    public async Task GetVersionAsync_WhenRepositoryThrows_Logs_Exception_And_Returns_Failure()
    {
        var repository = Substitute.For<ISettingsRepository>();
        var logger = Substitute.For<IDomainLogger>();
        var exception = new InvalidOperationException("config unreadable");
        repository.GetVersionAsync().Returns<Task<string?>>(_ => throw exception);
        var sut = new SettingsService(repository, logger);

        var result = await sut.GetVersionAsync();

        result.Status.Should().Be(ResultStatus.Failure);
        result.Error.Should().Be("config unreadable");
        logger.Received(1).Log(DomainLogLevel.Information, "Settings.GetVersion started.");
        logger.Received(1).Log(
            DomainLogLevel.Error,
            "Settings.GetVersion failed with an exception.",
            exception);
    }
}
