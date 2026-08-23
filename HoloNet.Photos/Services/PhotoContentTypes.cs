using HoloNet.Shared.Helpers;

namespace HoloNet.Photos.Services;

/// <summary>
/// Single source of truth for which file extensions count as a supported photo, and what HTTP
/// content-type to serve them as. Previously duplicated — and already out of sync — between
/// <see cref="PhotoService"/> (which extensions to scan for; <c>.bmp</c> was missing) and the
/// <c>{id}/image</c> endpoint in Program.cs (which content-type to serve; <c>.bmp</c> was
/// supported there but <c>.png</c> had no explicit case). Keeping both concerns in one place
/// means adding/removing a supported image format can't cause them to drift apart again. Built
/// on the shared <see cref="ContentTypeMap"/> lookup/fallback behavior used identically by
/// HoloNet.Games and HoloNet.Video.
/// </summary>
public static class PhotoContentTypes
{
    private static readonly ContentTypeMap Map = new(new Dictionary<string, string>
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp"
    }, fallbackContentType: "image/png");

    public static bool IsSupported(string filePath) => Map.IsSupported(filePath);

    /// <summary>Falls back to <c>image/png</c> for an unrecognized extension.</summary>
    public static string GetContentType(string filePathOrExtension) => Map.GetContentType(filePathOrExtension);
}

