using Looma.Domain.Entities;

namespace Looma.Domain.Search;

public static class DocumentSearchSpec
{
    public static IEnumerable<Document> Apply(IEnumerable<Document> source, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        var words = query
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return source.Where(document => words.All(word =>
            document.Nickname.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            document.Id.ToString().Contains(word, StringComparison.OrdinalIgnoreCase)));
    }
}
