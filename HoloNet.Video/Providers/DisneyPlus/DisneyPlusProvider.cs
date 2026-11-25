using System.Net;
using HoloNet.Shared.Models;
using HoloNet.Video.Abstractions;
using HoloNet.Video.Configuration;
using Microsoft.Extensions.Options;

namespace HoloNet.Video.Providers.DisneyPlus;

/// <summary>
/// Catalog-only Disney+ provider. Looks up a series' display metadata - title, synopsis,
/// artwork, every season and every episode - from Disney+'s public, signed-out series pages
/// (<c>/{locale}/browse/entity-{id}</c>), parsed by <see cref="DisneyPlusPageParser"/>.
/// It never calls Disney+'s playback/DRM endpoints: each episode is an
/// <see cref="PlaybackType.External"/> deep link to its <c>/{locale}/play/{id}</c> page, which
/// the user's own signed-in browser plays.
///
/// Disney+'s content API rejects anonymous device tokens, so the server-rendered pages are the
/// only credential-free source. A page embeds one season's episodes at a time, so the other
/// seasons are fetched with the page's own <c>?season={seasonId}</c> query, in parallel.
///
/// <see cref="GetHomeAsync"/> and <see cref="SearchAsync"/> are intentionally not implemented,
/// matching the Prime Video provider: the user curates which titles the launcher shows (see
/// <see cref="Services.ILibraryService"/>).
/// </summary>
public class DisneyPlusProvider(IHttpClientFactory httpClientFactory, IOptions<DisneyPlusOptions> options)
    : ICuratedCatalogProvider
{
    public const string HttpClientName = "DisneyPlus";
    private readonly DisneyPlusOptions _options = options.Value;

    public MediaProvider Provider => MediaProvider.DisneyPlus;

    public string DisplayName => "Disney+";

    public string? ParseTitleId(string urlOrId) => DisneyPlusTitleLink.ParseTitleId(urlOrId);

    public Task<IReadOnlyList<MediaCollection>> GetHomeAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaCollection>>([]);

    public Task<IReadOnlyList<MediaItem>> SearchAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaItem>>([]);

    public async Task<MediaItem?> GetAsync(string providerMediaId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerMediaId))
        {
            return null;
        }

        var seriesUrl = SeriesUrl(providerMediaId);
        var page = await GetPageAsync(seriesUrl, cancellationToken);
        if (page is null)
        {
            return null;
        }

        var seasonEpisodes = await Task.WhenAll(page.Seasons.Select(season =>
            season.Id == page.SelectedSeasonId
                ? Task.FromResult(page.SelectedSeasonEpisodes)
                : GetSeasonEpisodesAsync(seriesUrl, season.Id, cancellationToken)));

        var posterUrl = ImageUrl(page.BackgroundImageId);
        var seasons = page.Seasons
            .Select((season, index) => ToSeason(season, seriesUrl, posterUrl, seasonEpisodes[index]))
            .ToList();

        return new MediaItem(
            ProviderMediaId: providerMediaId,
            Provider: MediaProvider.DisneyPlus,
            Title: page.Title,
            EntityType: MediaEntityType.Series,
            PlaybackType: PlaybackType.External,
            Description: page.Synopsis,
            PosterUrl: posterUrl,
            BackdropUrl: posterUrl,
            Year: page.Year,
            Genres: page.Genres,
            DeepLinkUrl: seriesUrl,
            Seasons: seasons);
    }

    private async Task<IReadOnlyList<DisneyPlusEpisode>> GetSeasonEpisodesAsync(
        string seriesUrl, string seasonId, CancellationToken cancellationToken)
    {
        var page = await GetPageAsync(SeasonUrl(seriesUrl, seasonId), cancellationToken);
        return page?.SelectedSeasonId == seasonId ? page.SelectedSeasonEpisodes : [];
    }

    private async Task<DisneyPlusSeriesPage?> GetPageAsync(string url, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return DisneyPlusPageParser.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    /// <summary>
    /// The page lists seasons only by name, with no artwork of their own, so each season shows
    /// a still from its first episode that has one, falling back to the series art.
    /// </summary>
    private MediaItem ToSeason(DisneyPlusSeason season, string seriesUrl, string? seriesPosterUrl, IReadOnlyList<DisneyPlusEpisode> episodes) =>
        new(
            ProviderMediaId: season.Id,
            Provider: MediaProvider.DisneyPlus,
            Title: season.Name,
            EntityType: MediaEntityType.Season,
            PlaybackType: PlaybackType.External,
            PosterUrl: ImageUrl(episodes.FirstOrDefault(episode => episode.ImageId is not null)?.ImageId) ?? seriesPosterUrl,
            DeepLinkUrl: SeasonUrl(seriesUrl, season.Id),
            SequenceNumber: season.Number,
            Episodes: episodes.Select(ToEpisode).ToList());

    private MediaItem ToEpisode(DisneyPlusEpisode episode) =>
        new(
            ProviderMediaId: episode.Id,
            Provider: MediaProvider.DisneyPlus,
            Title: episode.Title,
            EntityType: MediaEntityType.Episode,
            PlaybackType: PlaybackType.External,
            Description: episode.Summary,
            PosterUrl: ImageUrl(episode.ImageId),
            DeepLinkUrl: $"{_options.GetBaseUrl()}/{_options.Locale}/play/{Uri.EscapeDataString(episode.Id)}",
            SequenceNumber: episode.Number);

    private string SeriesUrl(string entityId) =>
        $"{_options.GetBaseUrl()}/{_options.Locale}/browse/entity-{Uri.EscapeDataString(entityId)}";

    private static string SeasonUrl(string seriesUrl, string seasonId) =>
        $"{seriesUrl}?season={Uri.EscapeDataString(seasonId)}";

    /// <summary>Disney+'s image CDN ("ripcut") composes any width on request; consumers
    /// re-size by rewriting the <c>width</c> query, so a full-HD default is returned here.</summary>
    private static string? ImageUrl(string? ripcutId) =>
        string.IsNullOrWhiteSpace(ripcutId)
            ? null
            : $"https://disney.images.edge.bamgrid.com/ripcut-delivery/v2/variant/disney/{Uri.EscapeDataString(ripcutId)}/compose?format=jpeg&width=1920";
}
