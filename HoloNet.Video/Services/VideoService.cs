using System.Text;
using HoloNet.Video.Configuration;
using HoloNet.Video.Models;
using Microsoft.AspNetCore.WebUtilities;
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
        
        var videoFileNames = Directory.GetFiles(_videoServiceOptions.VideoPath)
            .Where(x => validExtensions.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase));


        List<VideoDto> videos = [];
        foreach (var filename in videoFileNames)
        {
            var fileInfo = new FileInfo(filename);
            var fileSize = fileInfo.Length;

            var urlSafeId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(filename));

            var streamUrl = $"{_videoServiceOptions.BaseUrl}/{urlSafeId}/stream";

            VideoDto video = new VideoDto(urlSafeId, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
                fileInfo.LastWriteTimeUtc, fileSize, streamUrl
            );
            videos.Add(video);
        }

        return Task.FromResult<IEnumerable<VideoDto>>(videos);
    }

    public Task<Stream?> GetStreamAsync(string id)
    {
        var bytes = WebEncoders.Base64UrlDecode(id);

        var filename = Encoding.UTF8.GetString(bytes);
        
        if (!File.Exists(filename))
            return Task.FromResult<Stream?>(null);

        var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);


        return Task.FromResult<Stream?>(stream);
    }

    public Task<VideoDto?> GetAsync(string id)
    {
        var bytes = WebEncoders.Base64UrlDecode(id);
        var filename = Encoding.UTF8.GetString(bytes);
        
        if (!File.Exists(filename))
        {
            return Task.FromResult<VideoDto?>(null);
        }

        var urlSafeId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(filename));
        

        var fileInfo = new FileInfo(filename);
        var streamUrl = $"{_videoServiceOptions.BaseUrl}/{urlSafeId}/stream";

        var metadata = new VideoDto(id, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc, fileInfo.Length, streamUrl
        );

        return Task.FromResult<VideoDto?>(metadata);
    }
}