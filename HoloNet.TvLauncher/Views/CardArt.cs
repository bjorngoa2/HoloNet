namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Tile proportions in the picker, following the 10-foot UI convention of landscape "banners"
/// for apps/destinations and portrait "posters" for content with box/cover art (games, films).
/// </summary>
public enum CardShape
{
    /// <summary>16:9 landscape tile, e.g. an app or a folder.</summary>
    Banner,

    /// <summary>2:3 portrait tile, e.g. game box art or a film poster.</summary>
    Poster
}

/// <summary>
/// Presentation-only artwork for a tile that has no data model of its own to derive it from
/// (a folder or an app shortcut) - grouped as one parameter object rather than a long tail of
/// optional constructor parameters on <see cref="FolderCardViewModel"/>.
/// </summary>
/// <param name="IconGlyph">A <see cref="TileIcons"/> glyph drawn on the accent background when there's no thumbnail.</param>
/// <param name="AccentColor">Hex brand colour (e.g. <c>#FF0033</c>) used for the tile background and the ambient glow when focused.</param>
/// <param name="CaptionsArt">See <see cref="IPickerCard.CaptionsArt"/>.</param>
public sealed record CardArt(
    CardShape Shape = CardShape.Banner,
    string? ThumbnailUrl = null,
    string? BackdropUrl = null,
    string? IconGlyph = null,
    string? AccentColor = null,
    string? Description = null,
    bool CaptionsArt = false);
