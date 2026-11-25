using System.IO;
using System.Windows.Media.Imaging;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// The one place artwork is decoded, so tiles, the hero backdrop and the "last session"
/// preview all load images the same way.
/// </summary>
public static class ImageSources
{
    /// <summary>
    /// Loads an image from a URL, decoded at <paramref name="decodeWidth"/> pixels wide rather
    /// than the source's full resolution (decoding dozens of full-size posters wastes memory
    /// and makes focus animations stutter). Returns <c>null</c> for a missing, malformed or
    /// unreadable URL. Remote images download asynchronously; check
    /// <see cref="BitmapSource.IsDownloading"/> before relying on the pixels being ready.
    /// </summary>
    public static BitmapImage? FromUrl(string? url, int decodeWidth = 0)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = uri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = decodeWidth;
            image.EndInit();
            return image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UnauthorizedAccessException)
        {
            // Local files load synchronously, so a missing or corrupt one throws here.
            return null;
        }
    }

    /// <summary>
    /// Loads a local file, bypassing WPF's URI-keyed image cache: screenshots are overwritten
    /// in place after each session, and a cached decode would keep showing the stale image.
    /// The result is frozen, so it can be loaded on a background thread. Returns <c>null</c>
    /// for a missing, locked or corrupt file.
    /// </summary>
    public static BitmapImage? FromFileUncached(string path, int decodeWidth = 0)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.DecodePixelWidth = decodeWidth;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UnauthorizedAccessException or FileFormatException)
        {
            return null;
        }
    }
}
