using System.Text.RegularExpressions;

namespace HoloNet.Video.Providers.PrimeVideo;

/// <summary>
/// Recognises a Prime Video title in a pasted link - <c>primevideo.com/-/nb/detail/{id}/ref=...</c>,
/// <c>primevideo.com/detail/{slug}/{id}</c> or <c>amazon.{tld}/gp/video/detail/{id}</c> - or a
/// bare title id such as <c>0K16R3PLUFGC2JUE457C26O4OD</c>.
/// </summary>
internal static partial class PrimeVideoTitleLink
{
    public static string? ParseTitleId(string urlOrId)
    {
        var input = urlOrId.Trim();
        if (TitleIdPattern().IsMatch(input))
        {
            return input;
        }

        if (!TitleLink.TryParse(input, out var uri)
            || !(TitleLink.IsOnDomain(uri, "primevideo.com") || AmazonHostPattern().IsMatch(uri.Host)))
        {
            return null;
        }

        var segments = TitleLink.PathSegments(uri);
        var detailIndex = Array.FindIndex(segments, segment => segment is "detail" or "dp");
        return detailIndex < 0
            ? null
            : segments.Skip(detailIndex + 1).Take(2).FirstOrDefault(TitleIdPattern().IsMatch);
    }

    /// <summary>Prime's own ids (26 characters) and Amazon ASINs (10, e.g. <c>B0D8...</c>) are upper-case alphanumeric.</summary>
    [GeneratedRegex("^[0-9A-Z]{10,32}$")]
    private static partial Regex TitleIdPattern();

    [GeneratedRegex(@"(^|\.)amazon\.[a-z.]+$", RegexOptions.IgnoreCase)]
    private static partial Regex AmazonHostPattern();
}
