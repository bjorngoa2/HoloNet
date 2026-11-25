namespace HoloNet.TvLauncher.Models;

/// <summary>
/// Mirrors the JSON shape of <c>HoloNet.Shared.Models.MediaProgress</c>, embedded directly on a
/// <c>MediaItemDto</c> as returned by <c>HoloNet.Video</c>'s catalog endpoints (see
/// <see cref="MediaItemDto.Progress"/>). Kept as a local copy per HoloNet's
/// no-service-to-service-dependency convention (see <see cref="MediaItemDto"/>).
/// </summary>
public record MediaProgressDto(
    double PercentWatched,
    string? ResumeLabel,
    int? ResumeTimeSeconds
);
