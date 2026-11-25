using HoloNet.Shared.Helpers;

namespace HoloNet.Video.Configuration;

/// <summary>
/// Configuration for the Prime Video catalog provider. This is catalog/browsing-only — see
/// <c>HoloNet.Video/Providers/PrimeVideo</c> for what it is (and deliberately isn't) used for.
/// </summary>
public class PrimeVideoOptions
{
    public string Region { get; set; } = "eu";
    public string BaseUrl { get; set; } = "https://www.primevideo.com";

    /// <summary>
    /// The raw <c>Cookie</c> header value from a logged-in browser session on primevideo.com.
    /// There is no official third-party auth flow for this, so for now this must be captured
    /// manually (DevTools -> Network -> any request to primevideo.com -> copy the Cookie header)
    /// and supplied via the <c>PrimeVideo__SessionCookie</c> environment variable or user-secrets
    /// — never commit it to appsettings.json. It expires (no refresh token); when catalog calls
    /// start failing with 401s, capture a fresh one the same way.
    /// </summary>
    public string SessionCookie { get; set; } = string.Empty;

    /// <summary>
    /// Path to this service's own SQLite database (relative to the app's content root, or
    /// absolute), where a runtime-captured session cookie is persisted (see
    /// <see cref="Infrastructure.PrimeVideoSessionStore"/> and
    /// <see cref="Infrastructure.HoloNetVideoDbContext"/>), so it survives container restarts
    /// without needing <see cref="SessionCookie"/> re-supplied via the environment. Defaults to
    /// <c>data/holonet-video.db</c> — inside the already-gitignored <c>HoloNet.Video/data/</c>
    /// directory, so the captured cookie is never accidentally committed.
    /// </summary>
    public string DbPath { get; set; } = "data/holonet-video.db";

    /// <summary>Whether a session cookie has been supplied. The Prime Video provider is skipped
    /// entirely (rather than failing startup) when this is <c>false</c>, since this integration is
    /// optional/best-effort and not everyone running HoloNet will have set it up.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SessionCookie);

    public ServiceBaseUrl GetBaseUrl() => ServiceBaseUrl.From(BaseUrl);
}
