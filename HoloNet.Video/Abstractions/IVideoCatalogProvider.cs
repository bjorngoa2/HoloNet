using HoloNet.Shared.Models;

namespace HoloNet.Video.Abstractions;

/// <summary>
/// Catalog/browsing access to a single media source (a <see cref="MediaProvider"/>). This is
/// deliberately catalog-only: it never implies HoloNet is authorized to actually play the
/// content itself. Whether — and how — a <see cref="MediaItem"/> can be played is expressed by
/// its <see cref="PlaybackType"/>, not by anything on this interface. Providers without a
/// legitimate, authorized playback mechanism (e.g. Prime Video today) must only ever produce
/// <see cref="PlaybackType.External"/> items.
/// </summary>
public interface IVideoCatalogProvider
{
    /// <summary>Which provider this instance serves. Must be unique across registered providers.</summary>
    MediaProvider Provider { get; }

    /// <summary>The home/landing page for this provider, grouped into named rows/carousels.</summary>
    Task<IReadOnlyList<MediaCollection>> GetHomeAsync(CancellationToken cancellationToken);

    /// <summary>Searches this provider's catalog for the given free-text query.</summary>
    Task<IReadOnlyList<MediaItem>> SearchAsync(string query, CancellationToken cancellationToken);

    /// <summary>Looks up a single item by its provider-specific id, or <c>null</c> if not found.</summary>
    Task<MediaItem?> GetAsync(string providerMediaId, CancellationToken cancellationToken);
}
