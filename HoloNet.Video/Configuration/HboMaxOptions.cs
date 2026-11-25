using HoloNet.Shared.Helpers;

namespace HoloNet.Video.Configuration;

/// <summary>
/// Configuration for the HBO Max catalog provider. This is catalog/browsing-only — see
/// <c>HoloNet.Video/Providers/HboMax</c> for what it is (and deliberately isn't) used for.
/// No credentials are needed: the catalog is read from HBO Max's public, signed-out title pages.
/// </summary>
public class HboMaxOptions
{
    /// <summary>The public site serving the title pages the catalog is read from.</summary>
    public string BaseUrl { get; set; } = "https://www.hbomax.com";

    /// <summary>The web player that episodes' and movies' deep links open in.</summary>
    public string PlayBaseUrl { get; set; } = "https://play.hbomax.com";

    /// <summary>
    /// The site's country segment (e.g. <c>no</c>). HBO Max's catalog differs per country, so it
    /// should be the country the user's subscription is in.
    /// </summary>
    public string Country { get; set; } = "no";

    /// <summary>The site's language segment (e.g. <c>en</c>), which picks the language of titles and synopses.</summary>
    public string Language { get; set; } = "en";

    public ServiceBaseUrl GetBaseUrl() => ServiceBaseUrl.From(BaseUrl);

    public ServiceBaseUrl GetPlayBaseUrl() => ServiceBaseUrl.From(PlayBaseUrl);
}
