namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Requests Disney+ artwork at the size it's displayed. Disney's image CDN ("ripcut") composes
/// each image at whatever <c>width</c> the URL's query asks for, so a tile only downloads the
/// pixels it shows.
/// </summary>
public static class DisneyPlusArtwork
{
    private const string CdnHost = "disney.images.edge.bamgrid.com";
    private const string WidthParameter = "width";

    /// <summary>
    /// Returns <paramref name="url"/> resized to <paramref name="width"/> pixels wide, or the URL
    /// unchanged when it isn't Disney+ CDN artwork.
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
