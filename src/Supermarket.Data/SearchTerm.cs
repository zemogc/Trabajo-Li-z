using System.Globalization;

namespace Supermarket.Data;

/// <summary>
/// Parameters for the "search by id or partial name" queries shared by every repository.
/// </summary>
internal readonly record struct SearchTerm(int? Id, string Pattern)
{
    public static SearchTerm Parse(string term)
    {
        ArgumentNullException.ThrowIfNull(term);

        var trimmed = term.Trim();
        int? id = int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;

        return new SearchTerm(id, $"%{EscapeLike(trimmed)}%");
    }

    /// <summary>
    /// Escapes LIKE wildcards so that user input such as "50%" is matched literally.
    /// </summary>
    internal static string EscapeLike(string value) => value
        .Replace("[", "[[]", StringComparison.Ordinal)
        .Replace("%", "[%]", StringComparison.Ordinal)
        .Replace("_", "[_]", StringComparison.Ordinal);
}
