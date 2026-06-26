using System.Text;
using System.Text.Json;
using HoloNet.Games.Configuration;
using HoloNet.Games.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace HoloNet.Games.Services;

public interface IGameService
{
    Task<IEnumerable<GameDto>> GetAllAsync();
    Task<GameDto?> GetAsync(string id);
    Task<Stream?> OpenReadAsync(string id);
}

public class GameService(IOptions<GameServiceOptions> options) : IGameService
{
    private readonly GameServiceOptions _gameServiceOptions = options.Value;

    public async Task<IEnumerable<GameDto>> GetAllAsync()
    {
        var files = Directory
            .EnumerateFiles(_gameServiceOptions.GamePath, "*", SearchOption.AllDirectories)
            .Where(x => string.Equals(Path.GetExtension(x), ".json", StringComparison.OrdinalIgnoreCase));

        List<GameDto> games = [];
        foreach (var filePath in files)
        {
            var fileInfo = new FileInfo(filePath);
            var urlSafeId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(filePath));
            var readUrl = $"{_gameServiceOptions.BaseUrl}/{urlSafeId}/game";

            await using var stream = File.OpenRead(filePath);
            var metadata = await JsonSerializer.DeserializeAsync<GameMetadata>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (metadata is null) continue;

            games.Add(new GameDto(
                urlSafeId,
                metadata.Title,
                metadata.Platform,
                metadata.Description,
                metadata.Year,
                fileInfo.Length,
                readUrl
            ));
        }

        return games;
    }

    public async Task<GameDto?> GetAsync(string id)
    {
        var bytes = WebEncoders.Base64UrlDecode(id);
        var filename = Encoding.UTF8.GetString(bytes);

        if (!File.Exists(filename))
        {
            return null;
        }

        var urlSafeId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(filename));
        var fileInfo = new FileInfo(filename);
        var readUrl = $"{_gameServiceOptions.BaseUrl}/{urlSafeId}/game";

        await using var fileStream = File.OpenRead(filename);
        var metadata = await JsonSerializer.DeserializeAsync<GameMetadata>(fileStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (metadata is null)
            return null;

        var gameDto = new GameDto(
            id,
            metadata.Title,
            metadata.Platform,
            metadata.Description,
            metadata.Year,
            fileInfo.Length,
            readUrl
        );

        return gameDto;
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

