namespace HoloNet.TvLauncher.Views;

/// <summary>
/// A "drill in" tile in the picker (see <see cref="IPickerCard"/>) — e.g. the root screen's
/// "Games" folder, a platform folder like "PS2" underneath it, or a Prime Video show/season.
/// Selecting one navigates into a child screen (see <see cref="MainWindow"/>'s selection
/// visitor) instead of launching anything.
/// </summary>
/// <param name="childrenFactory">
/// Lazily builds this folder's contents. Deferred (rather than built eagerly for every folder up
/// front) so, e.g., a platform folder's game-card list isn't constructed until the player
/// actually opens it.
/// </param>
/// <param name="navigationKey">
/// A stable identifier for re-finding "the same" folder in a freshly-rebuilt card tree after a
/// refresh (see <see cref="FolderNavigator"/>'s path-replay logic) - e.g. a Prime Video season's
/// <c>ProviderMediaId</c>. <c>null</c> for folders that never need this (Games' platform
/// folders), since re-deriving those after a refresh isn't needed today - unlike Prime Video
/// progress, game metadata doesn't change while the player is browsing.
/// </param>
/// <param name="art">
/// Optional artwork (e.g. a show's poster, or a brand icon/colour for a root destination).
/// Defaults to a folder icon on HoloNet's accent colour.
/// </param>
public class FolderCardViewModel(
    string title,
    string subtitle,
    Func<List<IPickerCard>> childrenFactory,
    string? navigationKey = null,
    CardArt? art = null) : PickerCardBase
{
    private readonly CardArt _art = art ?? new CardArt(IconGlyph: TileIcons.Folder);

    public override string Title { get; } = title;

    public override string Subtitle { get; } = subtitle;

    public string? NavigationKey { get; } = navigationKey;

    public override string? Description => _art.Description;

    public override string? ThumbnailUrl => _art.ThumbnailUrl;

    public override string? BackdropUrl => _art.BackdropUrl;

    public override string? IconGlyph => _art.IconGlyph;

    public override string AccentColor => _art.AccentColor ?? DefaultAccentColor;

    public override CardShape Shape => _art.Shape;

    public override bool CaptionsArt => _art.CaptionsArt;

    public List<IPickerCard> GetChildren() => childrenFactory();

    public override TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitFolder(this);
}
