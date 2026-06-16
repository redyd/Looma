// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Entities;
using Looma.Domain.Extensions;

namespace Looma.Domain.Search;

public class PatternSearchSpec : ISearchSpec<Pattern>
{
    public IEnumerable<Pattern> Apply(IEnumerable<Pattern> source, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        var words = query
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return source.Where(pattern => words.All(word =>
            pattern.Name.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            pattern.Type.GetDisplayName().Contains(word, StringComparison.OrdinalIgnoreCase) ||
            pattern.Type.ToString().Contains(word, StringComparison.OrdinalIgnoreCase) ||
            (pattern.IsPersonal ? "personnel" : "non personnel").Contains(word, StringComparison.OrdinalIgnoreCase) ||
            (pattern.Url?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (pattern.Note?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false)));
    }
}
