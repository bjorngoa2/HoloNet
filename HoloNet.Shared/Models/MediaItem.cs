namespace HoloNet.Shared.Models;

/// <summary>
/// A single piece of catalog metadata from any provider (local files, Prime Video, etc.).
/// This is a browsing/catalog record only — it does not imply HoloNet can actually play the
/// content itself. See <see cref="PlaybackType"/> on how to interpret <see cref="DeepLinkUrl"/>
/// vs. an in-app stream.
/// </summary>
public sealed record MediaItem
(
    string ProviderMediaId,
    MediaProvider Provider,
    string Title,
    MediaEntityType EntityType,
    PlaybackType PlaybackType,
    string? Description = null,
    string? PosterUrl = null,
    string? BackdropUrl = null,
    int? Year = null,
    IReadOnlyList<string>? Genres = null,
    /// <summary>Provider-supplied badge text, e.g. "Included with Prime".</summary>
    string? EntitlementMessage = null,
    /// <summary>Where to send the user to actually watch when <see cref="PlaybackType"/> is <see cref="Models.PlaybackType.External"/>.</summary>
    string? DeepLinkUrl = null,
    /// <summary>Season number (for a <see cref="MediaEntityType.Season"/>) or episode number
    /// (for a <see cref="MediaEntityType.Episode"/>), used purely for ordering/display.</summary>
    int? SequenceNumber = null,
    /// <summary>Every season of the show, populated only on a <see cref="MediaEntityType.Season"/>
    /// or <see cref="MediaEntityType.Series"/> item that has more than one season - lets a
    /// consumer drill from one season straight to its siblings without a separate lookup.</summary>
    IReadOnlyList<MediaItem>? Seasons = null,
    /// <summary>This season's episodes, populated only on a <see cref="MediaEntityType.Season"/> item.</summary>
    IReadOnlyList<MediaItem>? Episodes = null,
    /// <summary>"How far watched" for this specific item, when the provider's own page already
    /// reports one (e.g. Prime Video's detail page embeds each episode's own resume position
    /// directly - see <see cref="Video.Providers.PrimeVideo.PrimeVideoProvider"/>). <c>null</c>
    /// when the provider doesn't report this, or never started.</summary>
    MediaProgress? Progress = null
);
