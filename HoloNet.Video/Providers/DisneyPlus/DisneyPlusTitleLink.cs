using System.Text.RegularExpressions;

namespace HoloNet.Video.Providers.DisneyPlus;

/// <summary>
/// Recognises a Disney+ series in a pasted link - <c>disneyplus.com/{locale}/browse/entity-{id}</c>
/// - or a bare entity id such as <c>314f14b4-b70a-4ec6-b634-2559f0b1f77e</c>.
/// </summary>
internal static partial class DisneyPlusTitleLink
{
    private const string EntityPrefix = "entity-";

    public static string? ParseTitleId(string urlOrId)
    {
        var input = urlOrId.Trim();
        if (EntityIdPattern().IsMatch(input))
        {
            return input.ToLowerInvariant();
        }

        if (!TitleLink.TryParse(input, out var uri) || !TitleLink.IsOnDomain(uri, "disneyplus.com"))
        {
            return null;
        }

        return TitleLink.PathSegments(uri)
            .Where(segment => segment.StartsWith(EntityPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(segment => segment[EntityPrefix.Length..])
            .Where(id => EntityIdPattern().IsMatch(id))
            .Select(id => id.ToLowerInvariant())
            .FirstOrDefault();
    }

    [GeneratedRegex("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", RegexOptions.IgnoreCase)]
    private static partial Regex EntityIdPattern();
}
