using Looma.Domain.Entities;

namespace Looma.Domain.Search;

public static class WoolSearchSpec
{
    /// <summary>
    /// Recherche souple : chaque mot du query doit matcher au moins un champ.
    /// Ex : "drops rouge" → trouve les laines Drops de couleur rouge.
    /// </summary>
    public static IEnumerable<Wool> Apply(IEnumerable<Wool> source, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        var words = query
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return source.Where(w => words.All(word =>
            w.Name.Contains(word, StringComparison.OrdinalIgnoreCase)     ||
            w.Brand.Contains(word, StringComparison.OrdinalIgnoreCase)    ||
            w.Material.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            w.Color.Contains(word, StringComparison.OrdinalIgnoreCase)
        ));
    }
}