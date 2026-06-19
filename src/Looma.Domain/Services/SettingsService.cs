// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.IServices;
using Looma.Domain.Logging;
using Looma.Domain.Repositories;

namespace Looma.Domain.Services;

public sealed class SettingsService(ISettingsRepository repository, IDomainLogger logger)
    : DomainServiceBase(logger), ISettingsService
{
    public Task<ResultT<string?>> GetVersionAsync() =>
        ExecuteAsync("Settings.GetVersion", async () =>
            ResultT<string?>.Ok(await repository.GetVersionAsync()));

    public Task<Result> SetVersionAsync(string version) =>
        ExecuteAsync($"Settings.SetVersion({version})", async () =>
        {
            await repository.SetVersionAsync(version);
            return Result.Ok();
        });

    public Task<ResultT<string?>> GetReleaseNotesAsync(string version) =>
        ExecuteAsync($"Settings.GetReleaseNotes({version})", async () =>
            ResultT<string?>.Ok(await repository.GetReleaseNotesAsync(version)));

    public Task<Result> SetReleaseNotesAsync(string version, string releaseNotes) =>
        ExecuteAsync($"Settings.SetReleaseNotes({version})", async () =>
        {
            await repository.SetReleaseNotesAsync(version, releaseNotes);
            return Result.Ok();
        });

    public Task<ResultT<bool>> GetReleaseNotesShownAsync(string version) =>
        ExecuteAsync($"Settings.GetReleaseNotesShown({version})", async () =>
            ResultT<bool>.Ok(await repository.GetReleaseNotesShownAsync(version)));

    public Task<Result> SetReleaseNotesShownAsync(string version, bool shown) =>
        ExecuteAsync($"Settings.SetReleaseNotesShown({version}, {shown})", async () =>
        {
            await repository.SetReleaseNotesShownAsync(version, shown);
            return Result.Ok();
        });
}
