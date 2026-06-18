using System.Text;
using System.Text.Json;
using HoloNet.Games.Configuration;
using HoloNet.Games.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace HoloNet.Games.Services;

public interface IGameService
{
    Task<IEnumerable<GameMetadata>> GetAllAsync();
    Task<GameDto?> GetAsync(string id);
    Task<Stream?> OpenReadAsync(string id);
}

public class GameService(IOptions<GameServiceOptions> options) : IGameService
{
    private readonly GameServiceOptions _gameServiceOptions = options.Value;

    public async Task<IEnumerable<GameMetadata>> GetAllAsync()
    {
        //string[] validExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

        var files = Directory
            .EnumerateFiles(_gameServiceOptions.GamePath, "*", SearchOption.AllDirectories)
            .Where(x => string.Equals(Path.GetExtension(x), ".json", StringComparison.OrdinalIgnoreCase));


        List<GameMetadata> gamesMetaData = [];
        foreach (var filePath in files)
        {
            var fileInfo = new FileInfo(filePath);

            // TODO: Get filesize of the game ISO ?
            var fileSize = fileInfo.Length;

            var urlSafeId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(filePath));

            await using var stream = File.OpenRead(filePath);

            var streamUrl = $"{_gameServiceOptions.BaseUrl}/{urlSafeId}/game";
            var metadata = await JsonSerializer.DeserializeAsync<GameMetadata>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            /*GameDto game = new GameDto(urlSafeId, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc, fileInfo.LastWriteTimeUtc, fileSize, streamUrl);*/
            
            if (metadata is null) continue;
            
            metadata = metadata.SetFileSize(fileSize);
            gamesMetaData.Add(metadata);
        }

        return await Task.FromResult<IEnumerable<GameMetadata>>(gamesMetaData /*gamesMetaDataPaths*/);
    }

    public Task<GameDto?> GetAsync(string id)
    {
        var bytes = WebEncoders.Base64UrlDecode(id);


        var filename = Encoding.UTF8.GetString(bytes);

        if (!File.Exists(filename))
        {
            return Task.FromResult<GameDto?>(null);
        }

        var urlSafeId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(filename));

        var fileInfo = new FileInfo(filename);
        var readUrl = $"{_gameServiceOptions.BaseUrl}/{urlSafeId}/game";

        var photoMetadata = new GameDto(id, fileInfo.Name, fileInfo.Extension, fileInfo.CreationTimeUtc,
            fileInfo.LastWriteTimeUtc, fileInfo.Length, readUrl
        );

        return Task.FromResult<GameDto?>(photoMetadata);
    }


    public Task<Stream?> OpenReadAsync(string id)
    {
        var filename = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(id));

        if (!File.Exists(filename))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            filename,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        return Task.FromResult<Stream?>(stream);
    }
}

