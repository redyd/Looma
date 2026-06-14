// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Core;
using Looma.Domain.Entities;
using Looma.Domain.Extensions;

namespace Looma.Domain.Search;

public static class ProjectSearchSpec
{
    public static IEnumerable<Project> Apply(IEnumerable<Project> source, string? query, Status? status)
    {
        var filtered = status is null
            ? source
            : source.Where(project => project.Status == status.Value);

        if (string.IsNullOrWhiteSpace(query))
            return filtered;

        var words = query
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return filtered.Where(project => words.All(word =>
            project.Name.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            project.Status.GetDisplayName().Contains(word, StringComparison.OrdinalIgnoreCase) ||
            (project.Note?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false) ||
            MatchesDate(project.BeginDate, word) ||
            MatchesDate(project.EndDate, word) ||
            (project.Pattern?.Name.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (project.Pattern?.Url?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false) ||
            project.Wools.Any(usage => MatchesWool(usage.Wool, word))));
    }

    private static bool MatchesDate(DateOnly? date, string word) =>
        date is not null &&
        (date.Value.ToString("dd/MM/yyyy").Contains(word, StringComparison.OrdinalIgnoreCase) ||
         date.Value.ToString("yyyy-MM-dd").Contains(word, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesWool(Wool wool, string word) =>
        wool.Name.Contains(word, StringComparison.OrdinalIgnoreCase) ||
        wool.Brand.Contains(word, StringComparison.OrdinalIgnoreCase) ||
        wool.Material.Contains(word, StringComparison.OrdinalIgnoreCase) ||
        wool.Color.Contains(word, StringComparison.OrdinalIgnoreCase);
}
