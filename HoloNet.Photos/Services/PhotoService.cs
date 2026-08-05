using HoloNet.Photos.Configuration;
using HoloNet.Photos.Models;
using HoloNet.Shared.Helpers;
using Microsoft.Extensions.Options;

namespace HoloNet.Photos.Services;

public interface IPhotoService
{
    Task<IEnumerable<PhotoDto>> GetAllAsync();
    Task<PhotoDto?> GetAsync(string id);
    Task<Stream?> OpenReadAsync(string id);
}

public class PhotoService(IOptions<PhotoServiceOptions> options) : IPhotoService
{
    private readonly PhotoServiceOptions _photoServiceOptions = options.Value;

    public Task<IEnumerable<PhotoDto>> GetAllAsync()
    {
        string[] validExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

        var directory = _photoServiceOptions.GetPhotoDirectory();
        var baseUrl = _photoServiceOptions.GetBaseUrl();

        // Directory.EnumerateFiles has no async equivalent; offload the (potentially slow,
        // e.g. network share) scan to a background thread so it doesn't block the request thread.
        return Task.Run<IEnumerable<PhotoDto>>(() =>
        {
            var fileNames = Directory.EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories)
                .Where(x => validExtensions.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase));

            List<PhotoDto> photos = [];
            foreach (var filename in fileNames)
            {
                var fileInfo = new FileInfo(filename);
                var urlSafeId = FileId.Encode(filename);
                var imageUrl = $"{baseUrl}/{urlSafeId}/image";

                photos.Add(new PhotoDto(urlSafeId, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
                    fileInfo.LastWriteTimeUtc, fileInfo.Length, imageUrl));
            }

            return photos;
        });
    }

    public Task<PhotoDto?> GetAsync(string id)
    {
        var filename = FileId.TryDecode(id);
        if (filename is null || !_photoServiceOptions.GetPhotoDirectory().Contains(filename))
            return Task.FromResult<PhotoDto?>(null);

        if (!File.Exists(filename))
            return Task.FromResult<PhotoDto?>(null);

        var fileInfo = new FileInfo(filename);
        var readUrl = $"{_photoServiceOptions.GetBaseUrl()}/{FileId.Encode(filename)}/image";

        var photoMetadata = new PhotoDto(id, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc, fileInfo.Length, readUrl);

        return Task.FromResult<PhotoDto?>(photoMetadata);
    }

    public Task<Stream?> OpenReadAsync(string id)
    {
        var filename = FileId.TryDecode(id);
        if (filename is null || !_photoServiceOptions.GetPhotoDirectory().Contains(filename))
            return Task.FromResult<Stream?>(null);

        if (!File.Exists(filename))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }
}
