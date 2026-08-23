using HoloNet.Shared.Helpers;

namespace HoloNet.Games.Services;

/// <summary>
/// Single source of truth for which file extensions count as a game's cover-art/thumbnail
/// image, and what HTTP content-type to serve them as. Previously duplicated between
/// <see cref="GameService"/> (which extensions to look for on disk) and the
/// <c>api/v1/games/{id}/thumbnail</c> endpoint (which content-type to serve) — keeping both
/// concerns in one place means adding/removing a supported image format can't cause the two to
/// drift out of sync (a Shotgun Surgery risk otherwise). Built on the shared
/// <see cref="ContentTypeMap"/> lookup/fallback behavior used identically by
/// HoloNet.Video and HoloNet.Photos.
/// </summary>
public static class ThumbnailFormat
{
    private static readonly ContentTypeMap Map = new(new Dictionary<string, string>
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    }, fallbackContentType: "image/png");

    public static bool IsThumbnail(string filePath) => Map.IsSupported(filePath);

    /// <summary>Falls back to <c>image/png</c> for an unrecognized/missing extension.</summary>
    public static string GetContentType(string filePath) => Map.GetContentType(filePath);
}

