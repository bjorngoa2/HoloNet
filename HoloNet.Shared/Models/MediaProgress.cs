namespace HoloNet.Shared.Models;

/// <summary>
/// A provider's "how far through this title is the signed-in account" reading, embedded directly
/// in Prime Video's own <c>detail/{id}</c> page response (each episode's own
/// <c>action.btf[gti].primaryActions[0].payload.playback</c> block - see
/// <see cref="Video.Providers.PrimeVideo.PrimeVideoProvider"/> for the only source of this
/// today). No separate network call is needed: this rides along with the same detail-page
/// lookup <see cref="Video.Providers.PrimeVideo.PrimeVideoProvider.GetAsync"/> already makes for
/// title/synopsis/artwork - unlike the <c>enrichItemMetadata</c> endpoint initially tried for
/// this, which turned out to enrich an unrelated recommendations carousel rather than reliably
/// reporting a specific known title's actual watch history.
/// </summary>
public sealed record MediaProgress
(
    /// <summary>0-100. <c>0</c> for "never started".</summary>
    double PercentWatched,
    /// <summary>Provider-supplied label for the resume/play action, e.g. "Resume S1 E4" or
    /// "Play S1 E3" for an unstarted episode - shown as-is rather than reconstructed locally.</summary>
    string? ResumeLabel = null,
    /// <summary>Resume position in seconds, when the provider reports one.</summary>
    int? ResumeTimeSeconds = null
);

