namespace HoloNet.Video.Providers.HboMax;

/// <summary>
/// Recognises an HBO Max show or movie in a pasted link: a public title page
/// (<c>hbomax.com/no/en/show/{id}</c>, <c>/movie/{id}</c> - including a season's or an episode's
/// page, which names its show first - or an older <c>/shows/{slug}/{id}</c> link) or the web
/// player's own title page (<c>play.hbomax.com/show/{id}</c>). Bare ids aren't accepted: HBO Max
/// and Disney+ ids are both GUIDs, so only a link says which service one belongs to.
/// </summary>
internal static class HboMaxTitleLink
{
    private static readonly HashSet<string> TitleSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "show", "shows", "series", "movie", "movies", "feature",
    };

    public static string? ParseTitleId(string urlOrId)
    {
        if (!TitleLink.TryParse(urlOrId.Trim(), out var uri) || !TitleLink.IsOnDomain(uri, "hbomax.com"))
        {
            return null;
        }

        return TitleLink.PathSegments(uri)
            .SkipWhile(segment => !TitleSegments.Contains(segment))
            .Where(segment => HboMaxPageParser.IdPattern().IsMatch(segment))
            .Select(id => id.ToLowerInvariant())
            .FirstOrDefault();
    }
}
