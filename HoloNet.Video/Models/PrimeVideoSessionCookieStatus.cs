namespace HoloNet.Video.Models;

/// <summary>
/// Response body for <c>GET api/v1/admin/primevideo/session-cookie/status</c> — lets a caller
/// (e.g. HoloNet.TvLauncher, before deciding whether to prompt for a login refresh) check whether
/// a Prime Video session cookie is currently configured and when it was last captured, without
/// exposing the cookie value itself.
/// </summary>
public record PrimeVideoSessionCookieStatus(bool IsConfigured, DateTimeOffset? LastUpdatedUtc);
