namespace HoloNet.TvLauncher.Models;

/// <summary>
/// Mirrors the JSON shape of <c>HoloNet.Video.Models.LibraryTitleDto</c>: one hand-picked title
/// in a streaming folder, as returned by <c>api/v1/library</c>. <see cref="Provider"/> matches
/// <see cref="Views.StreamingService.CatalogProvider"/>, and <see cref="ProviderMediaId"/> is
/// the id to look the title up by in the catalog. Kept as a local copy per HoloNet's
/// no-service-to-service-dependency convention (see <see cref="MediaItemDto"/>).
/// </summary>
public record LibraryTitleDto(
    string Provider,
    string ProviderMediaId,
    string Title,
    string EntityType,
    string? PosterUrl,
    DateTimeOffset AddedUtc);