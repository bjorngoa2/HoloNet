using HoloNet.Shared.Helpers;

namespace HoloNet.Video.Services;

/// <summary>
/// Single source of truth for which file extensions count as a playable video, and what HTTP
/// content-type to serve them as. Previously duplicated between <see cref="VideoService"/>
/// (which extensions to scan for) and the <c>{id}/stream</c> endpoint in Program.cs (which
/// content-type to serve) — keeping both concerns in one place means adding/removing a
/// supported video format can't cause them to drift out of sync. Built on the shared
/// <see cref="ContentTypeMap"/> lookup/fallback behavior used identically by
/// HoloNet.Games and HoloNet.Photos.
/// </summary>
public static class VideoFileTypes
{
    private static readonly ContentTypeMap Map = new(new Dictionary<string, string>
    {
        [".mp4"] = "video/mp4",
        [".mkv"] = "video/x-matroska",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime"
    }, fallbackContentType: "application/octet-stream");

    public static bool IsSupported(string filePath) => Map.IsSupported(filePath);

    /// <summary>Falls back to <c>application/octet-stream</c> for an unrecognized extension.</summary>
    public static string GetContentType(string filePathOrExtension) => Map.GetContentType(filePathOrExtension);
}

