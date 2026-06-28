// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;

namespace Looma.Domain.IServices;

public interface ISettingsService
{
    Task<ResultT<string?>> GetSelectedLanguageAsync();
    Task<Result> SetSelectedLanguageAsync(string culture);
    Task<ResultT<string?>> GetVersionAsync();
    Task<Result> SetVersionAsync(string version);
    Task<ResultT<string?>> GetReleaseNotesAsync(string version);
    Task<Result> SetReleaseNotesAsync(string version, string releaseNotes);
    Task<ResultT<bool>> GetReleaseNotesShownAsync(string version);
    Task<Result> SetReleaseNotesShownAsync(string version, bool shown);
}
