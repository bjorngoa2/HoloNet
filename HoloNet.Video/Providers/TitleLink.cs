namespace HoloNet.Video.Providers;

/// <summary>Shared handling of title links pasted by the user (see <see cref="Abstractions.ICuratedCatalogProvider"/>).</summary>
internal static class TitleLink
{
    /// <summary>
    /// Reads a pasted link as an absolute http(s) URL, also accepting one copied without its
    /// scheme (<c>www.example.com/...</c>).
    /// </summary>
    public static bool TryParse(string input, out Uri uri)
    {
        var text = input.Contains("://", StringComparison.Ordinal) ? input : $"https://{input}";
        return Uri.TryCreate(text, UriKind.Absolute, out uri!) && uri.Scheme is "http" or "https";
    }

    /// <summary>Whether <paramref name="uri"/> is on <paramref name="domain"/> or one of its subdomains.</summary>
    public static bool IsOnDomain(Uri uri, string domain) =>
        uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase)
        || uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase);

    public static string[] PathSegments(Uri uri) =>
        uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
}
