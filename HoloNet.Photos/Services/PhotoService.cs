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
        var directory = _photoServiceOptions.GetPhotoDirectory();

        // Directory.EnumerateFiles has no async equivalent; offload the (potentially slow,
        // e.g. network share) scan to a background thread so it doesn't block the request thread.
        return Task.Run<IEnumerable<PhotoDto>>(() =>
        {
            var fileNames = Directory.EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories)
                .Where(PhotoContentTypes.IsSupported);

            List<PhotoDto> photos = [];
            foreach (var filename in fileNames)
                photos.Add(BuildDto(FileId.Encode(filename), filename));

            return photos;
        });
    }

    /// <summary>
    /// Resolves a photo id to its absolute file path, or <c>null</c> if the id is malformed,
    /// doesn't decode to a path within <see cref="PhotoServiceOptions.GetPhotoDirectory"/> (the
    /// path-traversal guard shared by every endpoint that accepts a photo id), or the file no
    /// longer exists.
    /// </summary>
    private string? ResolveFilePath(string id)
    {
        var filename = FileId.TryDecode(id);
        return filename is not null && _photoServiceOptions.GetPhotoDirectory().Contains(filename) && File.Exists(filename)
            ? filename
            : null;
    }

    /// <summary>
    /// Builds the API-facing <see cref="PhotoDto"/> for a photo whose id has already been
    /// resolved to a file path. Shared by <see cref="GetAllAsync"/> and <see cref="GetAsync"/>
    /// so the mapping from file metadata to DTO can't drift between them.
    /// </summary>
    private PhotoDto BuildDto(string id, string filename)
    {
        var fileInfo = new FileInfo(filename);
        var imageUrl = $"{_photoServiceOptions.GetBaseUrl()}/{id}/image";

        return new PhotoDto(id, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc, fileInfo.Length, imageUrl);
    }

    public Task<PhotoDto?> GetAsync(string id)
    {
        var filename = ResolveFilePath(id);
        return Task.FromResult(filename is null ? null : BuildDto(id, filename));
    }

    public Task<Stream?> OpenReadAsync(string id)
    {
        var filename = ResolveFilePath(id);
        if (filename is null)
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }
}
