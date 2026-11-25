namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Requests Prime Video artwork at the size it's displayed. Amazon's image CDN serves the
/// original upload by default (often an 8 MB, 2560px PNG), but resizes and re-encodes to JPEG
/// on request when a <c>._SX{width}_</c> modifier is inserted before the file extension. That
/// cuts a tile's download about 100-fold, so a screen of titles loads quickly over home Wi-Fi.
/// </summary>
public static class PrimeVideoArtwork
{
    private const string CdnHost = "m.media-amazon.com";
    private const string ImagePathPrefix = "/images/";
    private const string ModifierMarker = "._";

    /// <summary>
    /// Returns <paramref name="url"/> resized to <paramref name="width"/> pixels wide, or the URL
    /// unchanged when it isn't Amazon CDN artwork or already carries a size modifier.
    /// </summary>
    public static string? Sized(string? url, int width)
    {
        if (url is null
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !uri.Host.Equals(CdnHost, StringComparison.OrdinalIgnoreCase)
            || !uri.AbsolutePath.StartsWith(ImagePathPrefix, StringComparison.Ordinal))
        {
            return url;
        }

        var path = uri.AbsolutePath;
        var fileNameStart = path.LastIndexOf('/') + 1;
        var extensionStart = path.LastIndexOf('.');
        if (extensionStart <= fileNameStart || path.IndexOf(ModifierMarker, fileNameStart, StringComparison.Ordinal) >= 0)
            return url;

        var sizedPath = $"{path[..extensionStart]}._SX{width}_{path[extensionStart..]}";
        return new UriBuilder(uri) { Path = sizedPath }.Uri.ToString();
    }
}
