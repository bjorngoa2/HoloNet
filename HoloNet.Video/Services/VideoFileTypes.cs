namespace HoloNet.Video.Services;

/// <summary>
/// Single source of truth for which file extensions count as a playable video, and what HTTP
/// content-type to serve them as. Previously duplicated between <see cref="VideoService"/>
/// (which extensions to scan for) and the <c>{id}/stream</c> endpoint in Program.cs (which
/// content-type to serve) — keeping both concerns in one place means adding/removing a
/// supported video format can't cause them to drift out of sync.
/// </summary>
public static class VideoFileTypes
{
    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp4"] = "video/mp4",
        [".mkv"] = "video/x-matroska",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime"
    };

    public static bool IsSupported(string filePath) =>
        ContentTypesByExtension.ContainsKey(Path.GetExtension(filePath));

    /// <summary>Falls back to <c>application/octet-stream</c> for an unrecognized extension.</summary>
    public static string GetContentType(string filePathOrExtension) =>
        ContentTypesByExtension.GetValueOrDefault(Path.GetExtension(filePathOrExtension), "application/octet-stream");
}
