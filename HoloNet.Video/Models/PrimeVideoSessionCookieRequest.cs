namespace HoloNet.Video.Models;

/// <summary>
/// Request body for <c>PUT api/v1/admin/primevideo/session-cookie</c> — pushed by the
/// <c>HoloNet.PrimeAuth</c> browser-login tool after it captures a fresh Prime Video session
/// cookie (see <see cref="Infrastructure.PrimeVideoSessionStore"/>).
/// </summary>
public record PrimeVideoSessionCookieRequest(string Cookie);
