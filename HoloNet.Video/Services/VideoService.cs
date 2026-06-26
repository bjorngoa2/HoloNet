using HoloNet.Shared.Helpers;
using HoloNet.Video.Configuration;
using HoloNet.Video.Models;
using Microsoft.Extensions.Options;

namespace HoloNet.Video.Services;

public interface IVideoService
{
    Task<IEnumerable<VideoDto>> GetAllAsync();
    Task<Stream?> GetStreamAsync(string id);
    Task<VideoDto?> GetAsync(string id);
}

public class VideoService(IOptions<VideoServiceOptions> options) : IVideoService
{
    private readonly VideoServiceOptions _videoServiceOptions = options.Value;

    public Task<IEnumerable<VideoDto>> GetAllAsync()
    {
        string[] validExtensions = [".mp4", ".mkv", ".avi", ".mov"];

        var directory = _videoServiceOptions.GetVideoDirectory();
        var baseUrl = _videoServiceOptions.GetBaseUrl();

        var videoFileNames = Directory.EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories)
            .Where(x => validExtensions.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase));

        List<VideoDto> videos = [];
        foreach (var filename in videoFileNames)
        {
            var fileInfo = new FileInfo(filename);
            var urlSafeId = FileId.Encode(filename);
            var streamUrl = $"{baseUrl}/{urlSafeId}/stream";

            videos.Add(new VideoDto(urlSafeId, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
                fileInfo.LastWriteTimeUtc, fileInfo.Length, streamUrl));
        }

        return Task.FromResult<IEnumerable<VideoDto>>(videos);
    }

    public Task<Stream?> GetStreamAsync(string id)
    {
        var filename = FileId.TryDecode(id);
        if (filename is null)
            return Task.FromResult<Stream?>(null);

        if (!File.Exists(filename))
            return Task.FromResult<Stream?>(null);

        var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Task.FromResult<Stream?>(stream);
    }

    public Task<VideoDto?> GetAsync(string id)
    {
        var filename = FileId.TryDecode(id);
        if (filename is null)
            return Task.FromResult<VideoDto?>(null);

        if (!File.Exists(filename))
            return Task.FromResult<VideoDto?>(null);

        var fileInfo = new FileInfo(filename);
        var streamUrl = $"{_videoServiceOptions.GetBaseUrl()}/{FileId.Encode(filename)}/stream";

        var metadata = new VideoDto(id, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc, fileInfo.Length, streamUrl);

        return Task.FromResult<VideoDto?>(metadata);
    }
}
