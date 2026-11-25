namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Requests HBO Max artwork at the size it's displayed. HBO Max's image CDN resizes each image to
/// whatever width the URL's <c>w</c> query asks for (its originals are 4K), so a tile only
/// downloads the pixels it shows.
/// </summary>
public static class HboMaxArtwork
{
    private const string CdnHost = "beam-images.warnermediacdn.com";
    private const string WidthParameter = "w";

    /// <summary>
    /// Returns <paramref name="url"/> resized to <paramref name="width"/> pixels wide, or the URL
    /// unchanged when it isn't HBO Max CDN artwork.
    /// </summary>
    public static string? Sized(string? url, int width)
    {
        if (url is null
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !uri.Host.Equals(CdnHost, StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var parameters = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(parameter => !parameter.StartsWith($"{WidthParameter}=", StringComparison.OrdinalIgnoreCase))
            .Append($"{WidthParameter}={width}");

        return new UriBuilder(uri) { Query = string.Join('&', parameters) }.Uri.ToString();
    }
}
