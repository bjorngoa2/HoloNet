namespace HoloNet.Shared.Models;

/// <summary>
/// The kind of content a <see cref="MediaItem"/> represents. Providers that only expose movies
/// (e.g. local files) can ignore the series-related values.
/// </summary>
public enum MediaEntityType
{
    Movie,
    Series,
    Season,
    Episode,
    Other,
}
