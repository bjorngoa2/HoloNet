using HoloNet.TvLauncher.Models;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Presentation wrapper around a <see cref="MediaItemDto"/> (a streaming title, e.g. from Prime
/// Video or Disney+, fetched via HoloNet.Video's catalog API - see
/// <see cref="Services.IMediaCatalogApiClient"/>) for binding
/// in the picker, alongside <see cref="GameCardViewModel"/> and
/// <see cref="ShortcutCardViewModel"/> (see <see cref="IPickerCard"/>). Unlike a game, "launching"
/// one just opens <see cref="MediaItemDto.DeepLinkUrl"/> in the browser - HoloNet never
/// touches a service's playback/DRM endpoints, so there's no in-app playback to manage here, same
/// reasoning as <see cref="ShortcutCardViewModel"/>. The <see cref="StreamingService"/> supplies
/// its branding and artwork sizing.
/// </summary>
public class MediaCardViewModel(MediaItemDto media, StreamingService service, string? parentTitle = null) : PickerCardBase
{
    /// <summary>
    /// Builds the right kind of card for a <see cref="MediaItemDto"/>: a drill-in
    /// <see cref="FolderCardViewModel"/> when it has more than one season to choose from, or an
    /// episode list to show, or a plain <see cref="MediaCardViewModel"/> (launches
    /// <see cref="MediaItemDto.DeepLinkUrl"/> directly) for a leaf item - a movie, a single-season
    /// show's lone season, or an individual episode. A season with exactly one season overall
    /// skips straight to its episode list rather than showing a redundant one-item "Seasons"
    /// folder in between. Folders keep the title's artwork and synopsis, so a show looks the
    /// same whether it's a folder or a leaf.
    /// </summary>
    public static IPickerCard Create(MediaItemDto media, StreamingService service) => Create(media, service, parentTitle: null);

    private static IPickerCard Create(MediaItemDto media, StreamingService service, string? parentTitle)
    {
        if (media.Seasons is { Count: > 1 } seasons)
        {
            return new FolderCardViewModel(
                media.Title,
                $"{seasons.Count} seasons",
                () => seasons.OrderBy(s => s.SequenceNumber ?? 0).Select(season => Create(season, service)).ToList(),
                navigationKey: media.ProviderMediaId,
                art: ArtFor(media, service));
        }

        if (media.Episodes is { Count: > 0 } episodes)
        {
            return new FolderCardViewModel(
                media.Title,
                $"{episodes.Count} episode{(episodes.Count == 1 ? "" : "s")}",
                () => episodes.OrderBy(e => e.SequenceNumber ?? 0).Select(episode => Create(episode, service, media.Title)).ToList(),
                navigationKey: media.ProviderMediaId,
                art: ArtFor(media, service));
        }

        return new MediaCardViewModel(media, service, parentTitle);
    }

    private static CardArt ArtFor(MediaItemDto media, StreamingService service) => new(
        ThumbnailUrl: TileArtUrl(media, service),
        BackdropUrl: BackdropArtUrl(media, service),
        IconGlyph: TileIcons.Tv,
        AccentColor: service.AccentColor,
        Description: media.Description,
        // Seasons usually reuse the show's artwork, so their tiles need the name on them.
        CaptionsArt: media.EntityType == SeasonEntityType);

    private const int SeasonEntityType = 2;

    private static string? TileArtUrl(MediaItemDto media, StreamingService service) => service.TileArtUrl(media.PosterUrl);

    // Streaming artwork is landscape and high resolution, so it doubles as a crisp hero backdrop.
    private static string? BackdropArtUrl(MediaItemDto media, StreamingService service) =>
        service.BackdropArtUrl(media.BackdropUrl ?? media.PosterUrl);

    public MediaItemDto Media { get; } = media;

    public override string Title => Media.Title;

    /// <summary>
    /// Mirrors the ordinal values of <c>HoloNet.Shared.Models.MediaEntityType</c>
    /// (Movie=0, Series=1, Season=2, Episode=3, Other=4).
    /// </summary>
    private string EntityTypeLabel(int entityType) => entityType switch
    {
        0 => "Movie",
        1 => "Series",
        2 => "Season",
        3 => "Episode",
        _ => service.Name
    };

    public override string Subtitle => (Media.EntityType, Media.SequenceNumber) switch
    {
        (2, { } seasonNumber) => $"Season {seasonNumber}",
        (3, { } episodeNumber) => $"Episode {episodeNumber}",
        _ when Media.Year is { } year => $"{EntityTypeLabel(Media.EntityType)} · {year}",
        _ => EntityTypeLabel(Media.EntityType)
    };

    public override string? Description => Media.Description;

    /// <summary>
    /// The subtitle placed under the folder it was picked from, e.g. "Season 1 · Episode 1", for
    /// views shown outside the picker (like the playback controls) where that folder isn't visible.
    /// </summary>
    public string Breadcrumb => parentTitle is null ? Subtitle : $"{parentTitle} · {Subtitle}";

    public override string? ThumbnailUrl => TileArtUrl(Media, service);

    public override string? BackdropUrl => BackdropArtUrl(Media, service);

    public override string IconGlyph => TileIcons.Tv;

    public override string AccentColor => service.AccentColor;

    public override string ActionLabel => ProgressPercentage > 0 ? "Resume" : "Play";

    /// <summary>
    /// 0-100 "how far watched", read straight off <see cref="Media"/> - Prime's own detail-page
    /// response already embeds this for free alongside title/artwork (see
    /// <c>PrimeVideoProvider.GetAsync</c>/<c>ExtractEpisodes</c>), so no separate fetch or
    /// property-changed plumbing is needed here; it's just as static as <see cref="Title"/>.
    /// <c>null</c> when the provider doesn't report progress for this item, or it's unstarted.
    /// </summary>
    public override double? ProgressPercentage => Media.Progress?.PercentWatched;

    public override TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitMedia(this);
}
