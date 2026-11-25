namespace HoloNet.Shared.Models;

/// <summary>
/// A source that catalog data (and, where legitimately supported, playback) originates from.
/// New providers are added here as they're implemented — keep this list in sync with the
/// providers actually registered in <c>HoloNet.Video</c>.
/// </summary>
public enum MediaProvider
{
    Local,
    PrimeVideo,
    DisneyPlus,
    HboMax,
}
