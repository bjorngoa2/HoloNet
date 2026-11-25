using HoloNet.Shared.Helpers;

namespace HoloNet.Video.Configuration;

/// <summary>
/// Configuration for the Disney+ catalog provider. This is catalog/browsing-only — see
/// <c>HoloNet.Video/Providers/DisneyPlus</c> for what it is (and deliberately isn't) used for.
/// No credentials are needed: the catalog is read from Disney+'s public, signed-out series pages.
/// </summary>
public class DisneyPlusOptions
{
    public string BaseUrl { get; set; } = "https://www.disneyplus.com";

    /// <summary>
    /// The site locale segment (e.g. <c>en-gb</c>), which picks the language of titles and
    /// synopses and is reused in every deep link, so playback opens in the same language.
    /// </summary>
    public string Locale { get; set; } = "en-gb";

    public ServiceBaseUrl GetBaseUrl() => ServiceBaseUrl.From(BaseUrl);
}
