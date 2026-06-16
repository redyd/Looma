// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Entities;

namespace Looma.Domain.Search;

public class DocumentSearchSpec : ISearchSpec<Document>
{
    public IEnumerable<Document> Apply(IEnumerable<Document> source, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        var words = query
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return source.Where(document => words.All(word =>
            document.Nickname.Contains(word, StringComparison.OrdinalIgnoreCase)
            || document.Type.Contains(word, StringComparison.OrdinalIgnoreCase)));
    }
}
