namespace HoloNet.Photos.Services;

/// <summary>
/// Single source of truth for which file extensions count as a supported photo, and what HTTP
/// content-type to serve them as. Previously duplicated — and already out of sync — between
/// <see cref="PhotoService"/> (which extensions to scan for; <c>.bmp</c> was missing) and the
/// <c>{id}/image</c> endpoint in Program.cs (which content-type to serve; <c>.bmp</c> was
/// supported there but <c>.png</c> had no explicit case). Keeping both concerns in one place
/// means adding/removing a supported image format can't cause them to drift apart again.
/// </summary>
public static class PhotoContentTypes
{
    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp"
    };

    public static bool IsSupported(string filePath) =>
        ContentTypesByExtension.ContainsKey(Path.GetExtension(filePath));

    /// <summary>Falls back to <c>image/png</c> for an unrecognized extension.</summary>
    public static string GetContentType(string filePathOrExtension) =>
        ContentTypesByExtension.GetValueOrDefault(Path.GetExtension(filePathOrExtension), "image/png");
}
