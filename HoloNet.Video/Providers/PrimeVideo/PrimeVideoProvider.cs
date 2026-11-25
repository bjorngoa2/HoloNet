using System.Net;
using System.Net.Http.Json;
using HoloNet.Shared.Models;
using HoloNet.Video.Abstractions;
using HoloNet.Video.Configuration;
using HoloNet.Video.Infrastructure;
using Microsoft.Extensions.Options;

namespace HoloNet.Video.Providers.PrimeVideo;

/// <summary>
/// Catalog-only Prime Video provider. Looks up a single title's display metadata via Prime's
/// (undocumented, unofficial) <c>detail/{id}</c> page - title, description, poster, entitlement
/// badge, and a deep link back to primevideo.com. It never calls Prime's playback/DRM endpoints
/// (<c>GetVodPlaybackResources</c>, <c>GetWidevineLicense</c>) and only parses the page's SEO
/// <c>&lt;head&gt;</c> metadata (see <see cref="PrimeDetailResponse"/>), which deliberately never
/// touches the buy-box/playback-session data those pages also embed.
///
/// <see cref="GetHomeAsync"/> and <see cref="SearchAsync"/> are intentionally not implemented yet:
/// Prime's home-feed pagination requires ephemeral, page-session-bound tokens minted by their SPA
/// on each page load, and the first page of the feed is embedded in server-rendered HTML rather
/// than served from a clean JSON endpoint. Both are far more fragile than the single-item lookup
/// this provider currently supports - see BACKLOG.md (`prime-video-catalog`) for the reduced MVP
/// scope this was deliberately limited to.
///
/// Auth is a session cookie held by <see cref="PrimeVideoSessionStore"/>, captured either
/// manually (DevTools) or via the semi-automated <c>HoloNet.PrimeAuth</c> browser login tool -
/// there is no official third-party auth flow. When no cookie is configured, every method here
/// is a no-op rather than an error, since this provider is optional/best-effort.
/// </summary>
public class PrimeVideoProvider(IHttpClientFactory httpClientFactory, IOptions<PrimeVideoOptions> options, PrimeVideoSessionStore sessionStore)
    : ICuratedCatalogProvider
{
    private const string HttpClientName = "PrimeVideo";
    private readonly PrimeVideoOptions _options = options.Value;

    public MediaProvider Provider => MediaProvider.PrimeVideo;

    public string DisplayName => "Prime Video";

    public string? ParseTitleId(string urlOrId) => PrimeVideoTitleLink.ParseTitleId(urlOrId);

    public Task<IReadOnlyList<MediaCollection>> GetHomeAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaCollection>>([]);

    public Task<IReadOnlyList<MediaItem>> SearchAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaItem>>([]);

    public async Task<MediaItem?> GetAsync(string providerMediaId, CancellationToken cancellationToken) =>
        await GetAsync(providerMediaId, includeSeasonHierarchy: true, cancellationToken);

    /// <param name="includeSeasonHierarchy">
    /// Whether to also resolve <see cref="MediaItem.Seasons"/>/<see cref="MediaItem.Episodes"/>
    /// for a season/series item. <c>false</c> when fetching a sibling season (see below) so that
    /// doesn't recurse into fetching every other sibling's siblings in turn - each sibling only
    /// needs its own episode list, not the whole show's season list again.
    /// </param>
    private async Task<MediaItem?> GetAsync(string providerMediaId, bool includeSeasonHierarchy, CancellationToken cancellationToken)
    {
        if (!sessionStore.IsConfigured || string.IsNullOrWhiteSpace(providerMediaId))
        {
            return null;
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        var url = $"{_options.GetBaseUrl()}/region/{_options.Region}/detail/{Uri.EscapeDataString(providerMediaId)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Cookie", sessionStore.Cookie);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var detail = await response.Content.ReadFromJsonAsync<PrimeDetailResponse>(cancellationToken: cancellationToken);
        var mediaItem = detail?.Head is null ? null : ToMediaItem(providerMediaId, detail.Head);
        if (mediaItem is null)
        {
            return null;
        }

        var progress = ExtractProgress(detail!.Body?.Atf?.State?.Action, "atf", providerMediaId);
        if (progress is not null)
        {
            mediaItem = mediaItem with { Progress = progress };
        }

        if (!includeSeasonHierarchy)
        {
            return mediaItem with { Episodes = ExtractEpisodes(detail.Body) };
        }

        var episodes = ExtractEpisodes(detail.Body);
        var seasonStubs = ExtractSeasons(detail.Body);
        if (seasonStubs.Count == 0)
        {
            return episodes.Length == 0 ? mediaItem : mediaItem with { Episodes = episodes };
        }

        var seasons = new List<MediaItem>(seasonStubs.Count);
        foreach (var stub in seasonStubs)
        {
            if (stub.ProviderMediaId == providerMediaId)
            {
                seasons.Add(stub with { Episodes = episodes });
                continue;
            }

            // Prefer the season-selector's own displayName/sequenceNumber (consistent "season N"
            // labelling across every season) over that season's own SEO title, which varies in
            // formatting from page to page.
            var sibling = await GetAsync(stub.ProviderMediaId, includeSeasonHierarchy: false, cancellationToken);
            seasons.Add(sibling is null ? stub : sibling with { Title = stub.Title, SequenceNumber = stub.SequenceNumber });
        }

        return mediaItem with { Episodes = episodes, Seasons = seasons };
    }

    private MediaItem? ToMediaItem(string providerMediaId, PrimeDetailHead head)
    {
        var seo = head.SeoMetadata;
        if (seo is null)
        {
            return null;
        }

        var title = StripTitlePrefix(seo.Title);
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var description = seo.MetaTags?.FirstOrDefault(t => t.Name == "description")?.Content;
        var posterUrl = seo.MetaTagsRDFa?.FirstOrDefault(t => t.Property == "og:image")?.Content;
        var deepLinkUrl = seo.CanonicalUrl ?? $"{_options.GetBaseUrl()}/region/{_options.Region}/detail/{providerMediaId}";

        return new MediaItem(
            ProviderMediaId: providerMediaId,
            Provider: MediaProvider.PrimeVideo,
            Title: title,
            EntityType: ToEntityType(head.PageMetadata?.SubPageType),
            PlaybackType: PlaybackType.External,
            Description: description,
            PosterUrl: posterUrl,
            DeepLinkUrl: deepLinkUrl);
    }

    /// <summary>Prime's page &lt;title&gt; is formatted as "Prime Video: {title}" - strip that prefix
    /// so <see cref="MediaItem.Title"/> is just the title itself.</summary>
    private static string? StripTitlePrefix(string? seoTitle)
    {
        const string prefix = "Prime Video: ";
        return seoTitle is not null && seoTitle.StartsWith(prefix, StringComparison.Ordinal)
            ? seoTitle[prefix.Length..]
            : seoTitle;
    }

    /// <summary>
    /// Maps <c>body.atf.state.seasons</c> (a season-selector widget listing every season of the
    /// show, keyed by an arbitrary top-level title id) to lightweight <see cref="MediaItem"/>
    /// stubs - title/id/deep-link only, no episodes yet (see <see cref="GetAsync(string,bool,CancellationToken)"/>
    /// for how a sibling's own episodes get filled in).
    /// </summary>
    private List<MediaItem> ExtractSeasons(PrimeDetailBody? body)
    {
        var entries = body?.Atf?.State?.Seasons?.Values.FirstOrDefault();
        if (entries is null or { Count: 0 })
        {
            return [];
        }

        var seasons = new List<MediaItem>(entries.Count);
        foreach (var entry in entries)
        {
            var id = ExtractIdFromSeasonLink(entry.SeasonLink);
            if (id is null || string.IsNullOrWhiteSpace(entry.DisplayName))
            {
                continue;
            }

            seasons.Add(new MediaItem(
                ProviderMediaId: id,
                Provider: MediaProvider.PrimeVideo,
                Title: entry.DisplayName,
                EntityType: MediaEntityType.Season,
                PlaybackType: PlaybackType.External,
                DeepLinkUrl: $"{_options.GetBaseUrl()}{entry.SeasonLink}",
                SequenceNumber: entry.SequenceNumber));
        }

        return seasons;
    }

    /// <summary>Pulls the compact title id out of a <c>seasonLink</c> like
    /// <c>/region/eu/detail/0RTZ57DQ6PBHH29UN5JS7U7CW4?ref_=...</c>.</summary>
    private static string? ExtractIdFromSeasonLink(string? seasonLink)
    {
        if (string.IsNullOrWhiteSpace(seasonLink))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(seasonLink, "/detail/([^/?]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Maps <c>body.btf.state.episodeList</c> (ordered episode ids) cross-referenced with
    /// <c>body.btf.state.detail.detail</c> (per-episode title/synopsis/artwork, keyed by the same
    /// id) into this season's episode <see cref="MediaItem"/>s. Each episode's raw GTI
    /// (e.g. <c>amzn1.dv.gti.xxxx</c>) works directly as another <c>detail/{id}</c> request's id,
    /// same as a season/movie's shorter compact id - so no extra id-mapping step is needed.
    /// </summary>
    private MediaItem[] ExtractEpisodes(PrimeDetailBody? body)
    {
        var cardIds = body?.Btf?.State?.EpisodeList?.CardTitleIds;
        var details = body?.Btf?.State?.Detail?.Detail;
        if (cardIds is null or { Count: 0 } || details is null)
        {
            return [];
        }

        var actions = body?.Btf?.State?.Action;

        return cardIds
            .Select(id => details.TryGetValue(id, out var d) ? (Id: id, Detail: d) : ((string Id, PrimeEpisodeDetail Detail)?)null)
            .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.Value.Detail.Title))
            .Select(x => new MediaItem(
                ProviderMediaId: x!.Value.Id,
                Provider: MediaProvider.PrimeVideo,
                Title: x.Value.Detail.Title!,
                EntityType: MediaEntityType.Episode,
                PlaybackType: PlaybackType.External,
                Description: x.Value.Detail.Synopsis,
                PosterUrl: x.Value.Detail.Images?.Packshot,
                DeepLinkUrl: $"{_options.GetBaseUrl()}/region/{_options.Region}/detail/{x.Value.Id}",
                SequenceNumber: x.Value.Detail.EpisodeNumber,
                Progress: ExtractProgress(actions, "btf", x.Value.Id)))
            .ToArray();
    }

    /// <summary>
    /// Reads a single title/episode's "how far watched" straight out of the same
    /// <c>detail/{id}</c> response already fetched for its title/synopsis/artwork - this is
    /// where Prime's own web app sources its "Continue watching" progress bars, keyed by GTI
    /// under <c>body.{atf|btf}.state.action.{atf|btf}[gti].primaryActions[0].payload.playback</c>
    /// (confirmed via live HAR capture - see BACKLOG.md `progress-backend-provider`). No
    /// separate network call is needed, unlike the <c>enrichItemMetadata</c> endpoint originally
    /// (and incorrectly) assumed for this, which turned out to enrich an unrelated recommendation
    /// carousel rather than reliably reporting a specific known title's own watch history.
    /// </summary>
    /// <param name="actionsBySection">The <c>action</c> dictionary for a page section (e.g.
    /// <c>body.btf.state.action</c>), keyed by the same section name it's nested under.</param>
    /// <param name="sectionName">"atf" or "btf" - matches <paramref name="actionsBySection"/>'s
    /// single top-level key.</param>
    /// <param name="gti">The title/episode's Prime GTI to look up progress for.</param>
    private static MediaProgress? ExtractProgress(
        Dictionary<string, Dictionary<string, PrimeActionEntry>>? actionsBySection, string sectionName, string gti)
    {
        if (actionsBySection is null || !actionsBySection.TryGetValue(sectionName, out var byGti) || byGti is null)
        {
            return null;
        }

        if (!byGti.TryGetValue(gti, out var entry))
        {
            return null;
        }

        var action = entry.PrimaryActions?.FirstOrDefault();
        var playback = action?.Payload?.Playback;
        if (playback is null)
        {
            return null;
        }

        return new MediaProgress(
            PercentWatched: (playback.Progress ?? 0) * 100,
            ResumeLabel: action?.Presentation?.PrimaryLabel,
            ResumeTimeSeconds: playback.ResumeTime);
    }

    private static MediaEntityType ToEntityType(string? subPageType) => subPageType switch
    {
        "Movie" => MediaEntityType.Movie,
        "TVShow" or "Series" => MediaEntityType.Series,
        "Season" => MediaEntityType.Season,
        "Episode" => MediaEntityType.Episode,
        _ => MediaEntityType.Other,
    };
}
