namespace HoloNet.TvLauncher.Models;

/// <summary>
/// Mirrors <c>HoloNet.Games.Models.GameDto</c>. Kept as a local copy per HoloNet's
/// no-service-to-service-dependency convention — this is a standalone client, not a service.
/// </summary>
public record GameDto(
    string Id,
    string Title,
    string Platform,
    string? Description,
    int? Year,
    string[]? Genre,
    string? NetworkPath,
    long? FileSizeBytes
);
