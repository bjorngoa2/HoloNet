using HoloNet.Shared.Helpers;
using HoloNet.Video.Configuration;
using HoloNet.Video.Models;
using Microsoft.Extensions.Options;

namespace HoloNet.Video.Services;

public interface IVideoService
{
    Task<IEnumerable<VideoDto>> GetAllAsync();
    Task<VideoStream?> GetStreamAsync(string id);
    Task<VideoDto?> GetAsync(string id);
}

/// <summary>An open read stream for a video, paired with its file extension so the caller can
/// resolve a content-type (see <see cref="VideoFileTypes.GetContentType"/>) without a second
/// lookup back through <see cref="IVideoService.GetAsync"/>.</summary>
public sealed record VideoStream(Stream Stream, string Extension);

public class VideoService(IOptions<VideoServiceOptions> options) : IVideoService
{
    private readonly VideoServiceOptions _videoServiceOptions = options.Value;

    public Task<IEnumerable<VideoDto>> GetAllAsync()
    {
        var directory = _videoServiceOptions.GetVideoDirectory();
        var baseUrl = _videoServiceOptions.GetBaseUrl();

        // Directory.EnumerateFiles has no async equivalent; offload the (potentially slow,
        // e.g. network share) scan to a background thread so it doesn't block the request thread.
        return Task.Run<IEnumerable<VideoDto>>(() =>
        {
            var videoFileNames = Directory.EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories)
                .Where(VideoFileTypes.IsSupported);

            List<VideoDto> videos = [];
            foreach (var filename in videoFileNames)
            {
                var fileInfo = new FileInfo(filename);
                var urlSafeId = FileId.Encode(filename);
                var streamUrl = $"{baseUrl}/{urlSafeId}/stream";

                videos.Add(new VideoDto(urlSafeId, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
                    fileInfo.LastWriteTimeUtc, fileInfo.Length, streamUrl));
            }

            return videos;
        });
    }

    /// <summary>
    /// Resolves a video id to its absolute file path, or <c>null</c> if the id is malformed,
    /// doesn't decode to a path within <see cref="VideoServiceOptions.GetVideoDirectory"/> (the
    /// path-traversal guard shared by every endpoint that accepts a video id), or the file no
    /// longer exists.
    /// </summary>
    private string? ResolveFilePath(string id)
    {
        var filename = FileId.TryDecode(id);
        return filename is not null && _videoServiceOptions.GetVideoDirectory().Contains(filename) && File.Exists(filename)
            ? filename
            : null;
    }

    public Task<VideoStream?> GetStreamAsync(string id)
    {
        var filename = ResolveFilePath(id);
        if (filename is null)
            return Task.FromResult<VideoStream?>(null);

        var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);

        return Task.FromResult<VideoStream?>(new VideoStream(stream, Path.GetExtension(filename)));
    }

    public Task<VideoDto?> GetAsync(string id)
    {
        var filename = ResolveFilePath(id);
        if (filename is null)
            return Task.FromResult<VideoDto?>(null);

        var fileInfo = new FileInfo(filename);
        var streamUrl = $"{_videoServiceOptions.GetBaseUrl()}/{FileId.Encode(filename)}/stream";

        var metadata = new VideoDto(id, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc, fileInfo.Length, streamUrl);

        return Task.FromResult<VideoDto?>(metadata);
    }
}
