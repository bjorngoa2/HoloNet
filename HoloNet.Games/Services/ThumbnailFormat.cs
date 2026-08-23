namespace HoloNet.Games.Services;

/// <summary>
/// Single source of truth for which file extensions count as a game's cover-art/thumbnail
/// image, and what HTTP content-type to serve them as. Previously duplicated between
/// <see cref="GameService"/> (which extensions to look for on disk) and the
/// <c>api/v1/games/{id}/thumbnail</c> endpoint (which content-type to serve) — keeping both
/// concerns in one place means adding/removing a supported image format can't cause the two to
/// drift out of sync (a Shotgun Surgery risk otherwise).
/// </summary>
public static class ThumbnailFormat
{
    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    public static bool IsThumbnail(string filePath) =>
        ContentTypesByExtension.ContainsKey(Path.GetExtension(filePath));

    /// <summary>Falls back to <c>image/png</c> for an unrecognized/missing extension.</summary>
    public static string GetContentType(string filePath) =>
        ContentTypesByExtension.GetValueOrDefault(Path.GetExtension(filePath), "image/png");
}
