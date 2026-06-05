using Looma.Domain.Entities;

namespace Looma.Domain.Search;

public static class PatternSearchSpec
{
    public static IEnumerable<Pattern> Apply(IEnumerable<Pattern> source, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        var words = query
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return source.Where(pattern => words.All(word =>
            pattern.Name.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            (pattern.Url?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (pattern.Note?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false)));
    }
}
