namespace HoloNet.TvLauncher.Models;

/// <summary>
/// Mirrors the JSON shape of <c>HoloNet.Shared.Models.MediaItem</c>, as returned by
/// <c>HoloNet.Video</c>'s <c>GET api/v1/catalog/{provider}/{id}</c> endpoint. Kept as a local
/// copy per HoloNet's no-service-to-service-dependency convention (see <see cref="GameDto"/>) —
/// this is a standalone client, not a service, so it deserializes only the fields the picker
/// actually displays rather than referencing <c>HoloNet.Shared</c> directly.
/// </summary>
public record MediaItemDto(
    string ProviderMediaId,
    string Title,
    int EntityType,
    int PlaybackType,
    string? Description,
    string? PosterUrl,
    string? BackdropUrl,
    int? Year,
    string[]? Genres,
    string? EntitlementMessage,
    string? DeepLinkUrl,
    int? SequenceNumber = null,
    IReadOnlyList<MediaItemDto>? Seasons = null,
    IReadOnlyList<MediaItemDto>? Episodes = null,
    /// <summary>"How far watched", when the provider's own page already reports one for this
    /// specific item - see <c>HoloNet.Shared.Models.MediaProgress</c>. <c>null</c> when not
    /// reported or never started.</summary>
    MediaProgressDto? Progress = null
);
