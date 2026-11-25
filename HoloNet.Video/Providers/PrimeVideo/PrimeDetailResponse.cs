using System.Text.Json.Serialization;

namespace HoloNet.Video.Providers.PrimeVideo;

// Narrow, catalog-only view of Prime Video's detail/{id} response. Deliberately does not model
// "globalStore" (playbackEnvelope - live DRM session data) - only the page's <head> SEO
// metadata, season/episode-list widgets, and each item's own "primaryActions" resume/progress
// data are mapped here.
internal sealed class PrimeDetailResponse
{
    [JsonPropertyName("head")]
    public PrimeDetailHead? Head { get; set; }

    [JsonPropertyName("body")]
    public PrimeDetailBody? Body { get; set; }
}

// Narrow slice of "body" - the season-selector, episode-list, and per-item resume/progress
// action widgets (catalog/browsing data), never "buyBoxActions"/"playbackAction" DRM/entitlement
// session data, which is simply never modeled here, by construction rather than by convention.
internal sealed class PrimeDetailBody
{
    [JsonPropertyName("atf")]
    public PrimeDetailSection? Atf { get; set; }

    [JsonPropertyName("btf")]
    public PrimeDetailSection? Btf { get; set; }
}

internal sealed class PrimeDetailSection
{
    [JsonPropertyName("state")]
    public PrimeDetailState? State { get; set; }
}

internal sealed class PrimeDetailState
{
    /// <summary>Keyed by an arbitrary top-level title id - callers should just take the single
    /// value present (see <see cref="PrimeVideoProvider"/>).</summary>
    [JsonPropertyName("seasons")]
    public Dictionary<string, List<PrimeSeasonEntry>>? Seasons { get; set; }

    [JsonPropertyName("episodeList")]
    public PrimeEpisodeList? EpisodeList { get; set; }

    [JsonPropertyName("detail")]
    public PrimeDetailContainer? Detail { get; set; }

    /// <summary>Per-title "what does the Play/Resume button do" data, keyed by the same
    /// "atf"/"btf" name as the containing section (see <see cref="PrimeVideoProvider"/>'s
    /// progress lookup) - this is where each episode's own watch progress actually lives, not
    /// the <c>enrichItemMetadata</c> endpoint originally (and incorrectly) assumed for this.</summary>
    [JsonPropertyName("action")]
    public Dictionary<string, Dictionary<string, PrimeActionEntry>>? Action { get; set; }
}

internal sealed class PrimeActionEntry
{
    [JsonPropertyName("primaryActions")]
    public List<PrimeActionItem>? PrimaryActions { get; set; }
}

internal sealed class PrimeActionItem
{
    [JsonPropertyName("payload")]
    public PrimeActionPayload? Payload { get; set; }

    [JsonPropertyName("presentation")]
    public PrimeActionPresentation? Presentation { get; set; }
}

internal sealed class PrimeActionPayload
{
    [JsonPropertyName("playback")]
    public PrimePlaybackPayload? Playback { get; set; }
}

internal sealed class PrimePlaybackPayload
{
    /// <summary>0.0-1.0 fraction watched (multiply by 100 for a percentage).</summary>
    [JsonPropertyName("progress")]
    public double? Progress { get; set; }

    [JsonPropertyName("resumeTime")]
    public int? ResumeTime { get; set; }

    [JsonPropertyName("runTime")]
    public int? RunTime { get; set; }
}

internal sealed class PrimeActionPresentation
{
    [JsonPropertyName("primaryLabel")]
    public string? PrimaryLabel { get; set; }
}

internal sealed class PrimeSeasonEntry
{
    [JsonPropertyName("seasonLink")]
    public string? SeasonLink { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("sequenceNumber")]
    public int? SequenceNumber { get; set; }
}

internal sealed class PrimeEpisodeList
{
    /// <summary>Ordered list of episode GTIs (e.g. "amzn1.dv.gti.xxxx") - each one works directly
    /// as the <c>{id}</c> segment of another <c>detail/{id}</c> request, same as a season/movie's
    /// compact id (see <see cref="PrimeVideoProvider"/>).</summary>
    [JsonPropertyName("cardTitleIds")]
    public List<string>? CardTitleIds { get; set; }
}

internal sealed class PrimeDetailContainer
{
    /// <summary>Keyed by episode GTI - the per-episode title/synopsis/artwork that
    /// <see cref="PrimeEpisodeList.CardTitleIds"/> only lists ids for.</summary>
    [JsonPropertyName("detail")]
    public Dictionary<string, PrimeEpisodeDetail>? Detail { get; set; }
}

internal sealed class PrimeEpisodeDetail
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("synopsis")]
    public string? Synopsis { get; set; }

    [JsonPropertyName("episodeNumber")]
    public int? EpisodeNumber { get; set; }

    [JsonPropertyName("images")]
    public PrimeEpisodeImages? Images { get; set; }
}

internal sealed class PrimeEpisodeImages
{
    [JsonPropertyName("packshot")]
    public string? Packshot { get; set; }
}

internal sealed class PrimeDetailHead
{
    [JsonPropertyName("seoMetadata")]
    public PrimeSeoMetadata? SeoMetadata { get; set; }

    [JsonPropertyName("pageMetadata")]
    public PrimePageMetadata? PageMetadata { get; set; }
}

internal sealed class PrimeSeoMetadata
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("canonicalUrl")]
    public string? CanonicalUrl { get; set; }

    [JsonPropertyName("metaTags")]
    public List<PrimeMetaTag>? MetaTags { get; set; }

    [JsonPropertyName("metaTagsRDFa")]
    public List<PrimeMetaTagRdfa>? MetaTagsRDFa { get; set; }
}

internal sealed class PrimeMetaTag
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

internal sealed class PrimeMetaTagRdfa
{
    [JsonPropertyName("property")]
    public string? Property { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

internal sealed class PrimePageMetadata
{
    [JsonPropertyName("subPageType")]
    public string? SubPageType { get; set; }
}
