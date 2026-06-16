// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using Looma.Domain.Entities;

namespace Looma.Domain.Search;

public class WoolSearchSpec : ISearchSpec<Wool>
{
    /// <summary>
    /// Recherche souple : chaque mot du query doit matcher au moins un champ.
    /// Ex : "drops rouge" → trouve les laines Drops de couleur rouge.
    /// </summary>
    public IEnumerable<Wool> Apply(IEnumerable<Wool> source, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        var words = query
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return source.Where(w => words.All(word =>
            w.Name.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            w.Brand.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            w.Material.Contains(word, StringComparison.OrdinalIgnoreCase)
        ));
    }
}