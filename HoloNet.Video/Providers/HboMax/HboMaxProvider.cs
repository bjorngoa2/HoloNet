using System.Net;
using HoloNet.Shared.Models;
using HoloNet.Video.Abstractions;
using HoloNet.Video.Configuration;
using Microsoft.Extensions.Options;

namespace HoloNet.Video.Providers.HboMax;

/// <summary>
/// Catalog-only HBO Max provider. Looks up a show's or movie's display metadata - title,
/// synopsis, artwork, and for a show every season and every episode - from HBO Max's public,
/// signed-out title pages (<c>/{country}/{language}/show/{id}</c>, which the site redirects to
/// <c>/movie/{id}</c> for a movie), parsed by <see cref="HboMaxPageParser"/>.
/// It never calls HBO Max's playback/DRM endpoints: each episode is an
/// <see cref="PlaybackType.External"/> deep link to its <c>play.hbomax.com/video/watch/{id}</c>
/// player page, which the user's own signed-in browser plays. A movie links to its signed-in
/// <c>play.hbomax.com/movie/{id}</c> page instead: a movie's playable video has an ID of its own
/// that only signed-in pages reveal (its watch link with the movie's ID is "content not available").
///
/// HBO Max's content API refuses anonymous device tokens ("age restricted"), so the
/// server-rendered pages are the only credential-free source. A page embeds one season's episodes
/// at a time, so the other seasons are fetched from their own <c>/s{number}</c> pages, in parallel.
///
/// <see cref="GetHomeAsync"/> and <see cref="SearchAsync"/> are intentionally not implemented,
/// matching the other streaming providers: the user curates which titles the launcher shows (see
/// <see cref="Services.ILibraryService"/>).
/// </summary>
public class HboMaxProvider(IHttpClientFactory httpClientFactory, IOptions<HboMaxOptions> options)
    : ICuratedCatalogProvider
{
    public const string HttpClientName = "HboMax";

    /// <summary>HBO Max's image CDN resizes to the <c>w</c> query's width; its originals are 4K.</summary>
    private const int ImageWidth = 1920;

    private readonly HboMaxOptions _options = options.Value;

    public MediaProvider Provider => MediaProvider.HboMax;

    public string DisplayName => "HBO Max";

    public string? ParseTitleId(string urlOrId) => HboMaxTitleLink.ParseTitleId(urlOrId);

    public Task<IReadOnlyList<MediaCollection>> GetHomeAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaCollection>>([]);

    public Task<IReadOnlyList<MediaItem>> SearchAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaItem>>([]);

    public async Task<MediaItem?> GetAsync(string providerMediaId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerMediaId) || !HboMaxPageParser.IdPattern().IsMatch(providerMediaId))
        {
            return null;
        }

        var titleUrl = TitleUrl(providerMediaId);
        var page = await GetPageAsync(titleUrl, cancellationToken);
        if (page is null)
        {
            return null;
        }

        if (page.IsMovie)
        {
            return ToTitle(page, MediaEntityType.Movie, deepLinkUrl: PlayUrl("movie", page.Id), seasons: null);
        }

        var seasonEpisodes = await Task.WhenAll(page.Seasons.Select(season =>
            season.Episodes.Count > 0
                ? Task.FromResult(season.Episodes)
                : GetSeasonEpisodesAsync(titleUrl, season.Number, cancellationToken)));

        var seasons = page.Seasons
            .Select((season, index) => ToSeason(season.Number, titleUrl, page, seasonEpisodes[index]))
            .ToList();

        return ToTitle(page, MediaEntityType.Series, deepLinkUrl: titleUrl, seasons);
    }

    private async Task<IReadOnlyList<HboMaxEpisode>> GetSeasonEpisodesAsync(
        string titleUrl, int seasonNumber, CancellationToken cancellationToken)
    {
        var page = await GetPageAsync(SeasonUrl(titleUrl, seasonNumber), cancellationToken);
        return page?.Seasons.FirstOrDefault(season => season.Number == seasonNumber)?.Episodes ?? [];
    }

    private async Task<HboMaxTitlePage?> GetPageAsync(string url, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return HboMaxPageParser.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private static MediaItem ToTitle(HboMaxTitlePage page, MediaEntityType entityType, string deepLinkUrl, IReadOnlyList<MediaItem>? seasons) =>
        new(
            ProviderMediaId: page.Id,
            Provider: MediaProvider.HboMax,
            Title: page.Title,
            EntityType: entityType,
            PlaybackType: PlaybackType.External,
            Description: page.Summary,
            PosterUrl: ImageUrl(page.PosterUrl),
            BackdropUrl: ImageUrl(page.BackdropUrl),
            Year: page.Year,
            Genres: page.Genres,
            DeepLinkUrl: deepLinkUrl,
            Seasons: seasons);

    /// <summary>
    /// Seasons have no artwork of their own, so each season shows a still from its first episode
    /// that has one, falling back to the show's art.
    /// </summary>
    private MediaItem ToSeason(int number, string titleUrl, HboMaxTitlePage show, IReadOnlyList<HboMaxEpisode> episodes) =>
        new(
            ProviderMediaId: $"{show.Id}/s{number}",
            Provider: MediaProvider.HboMax,
            Title: $"Season {number}",
            EntityType: MediaEntityType.Season,
            PlaybackType: PlaybackType.External,
            PosterUrl: ImageUrl(episodes.FirstOrDefault(episode => episode.ImageUrl is not null)?.ImageUrl ?? show.BackdropUrl),
            DeepLinkUrl: SeasonUrl(titleUrl, number),
            SequenceNumber: number,
            Episodes: episodes.Select(ToEpisode).ToList());

    private MediaItem ToEpisode(HboMaxEpisode episode) =>
        new(
            ProviderMediaId: episode.Id,
            Provider: MediaProvider.HboMax,
            Title: episode.Title,
            EntityType: MediaEntityType.Episode,
            PlaybackType: PlaybackType.External,
            Description: episode.Summary,
            PosterUrl: ImageUrl(episode.ImageUrl),
            DeepLinkUrl: PlayUrl("video/watch", episode.Id),
            SequenceNumber: episode.Number);

    private string TitleUrl(string id) =>
        $"{_options.GetBaseUrl()}/{_options.Country}/{_options.Language}/show/{Uri.EscapeDataString(id.ToLowerInvariant())}";

    private static string SeasonUrl(string titleUrl, int seasonNumber) => $"{titleUrl}/s{seasonNumber}";

    private string PlayUrl(string path, string id) => $"{_options.GetPlayBaseUrl()}/{path}/{Uri.EscapeDataString(id)}";

    /// <summary>Requests a full-HD copy; consumers re-size by rewriting the <c>w</c> query.</summary>
    private static string? ImageUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : $"{url}{(url.Contains('?') ? '&' : '?')}w={ImageWidth}";
}
