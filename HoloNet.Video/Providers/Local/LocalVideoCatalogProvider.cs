using HoloNet.Shared.Models;
using HoloNet.Video.Abstractions;
using HoloNet.Video.Models;
using HoloNet.Video.Services;

namespace HoloNet.Video.Providers.Local;

/// <summary>
/// Adapts the existing <see cref="IVideoService"/> (NAS file scanning + streaming) into the
/// catalog abstraction, so local files can appear alongside other providers in a unified
/// browsing view. This does not change how local playback works — <see cref="VideoDto.StreamUrl"/>
/// (HoloNet's own <c>api/v1/videos/{id}/stream</c> endpoint) is carried through unchanged as the
/// <see cref="MediaItem"/>'s playback URL, and <see cref="PlaybackType.Native"/> tells callers
/// that's a real, in-app stream rather than a link out to a third party.
/// </summary>
public class LocalVideoCatalogProvider(IVideoService videoService) : IVideoCatalogProvider
{
    public MediaProvider Provider => MediaProvider.Local;

    public async Task<IReadOnlyList<MediaCollection>> GetHomeAsync(CancellationToken cancellationToken)
    {
        var videos = await videoService.GetAllAsync();
        var items = videos.Select(ToMediaItem).ToList();

        return [new MediaCollection("My Videos", items)];
    }

    public async Task<IReadOnlyList<MediaItem>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var videos = await videoService.GetAllAsync();

        return videos
            .Where(v => v.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(ToMediaItem)
            .ToList();
    }

    public async Task<MediaItem?> GetAsync(string providerMediaId, CancellationToken cancellationToken)
    {
        var video = await videoService.GetAsync(providerMediaId);
        return video is null ? null : ToMediaItem(video);
    }

    private static MediaItem ToMediaItem(VideoDto video) => new(
        ProviderMediaId: video.Id,
        Provider: MediaProvider.Local,
        Title: video.Title,
        EntityType: MediaEntityType.Movie,
        PlaybackType: PlaybackType.Native,
        DeepLinkUrl: video.StreamUrl);
}
