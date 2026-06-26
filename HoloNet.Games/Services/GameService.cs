using System.Text.Json;
using HoloNet.Games.Configuration;
using HoloNet.Games.Models;
using HoloNet.Shared.Helpers;
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

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IEnumerable<GameDto>> GetAllAsync()
    {
        var files = Directory
            .EnumerateFiles(_gameServiceOptions.GamePath, "*", SearchOption.AllDirectories)
            .Where(x => string.Equals(Path.GetExtension(x), ".json", StringComparison.OrdinalIgnoreCase));

        List<GameDto> games = [];
        foreach (var filePath in files)
        {
            var fileInfo = new FileInfo(filePath);
            var urlSafeId = FileId.Encode(filePath);
            var readUrl = $"{_gameServiceOptions.BaseUrl}/{urlSafeId}/game";

            await using var stream = File.OpenRead(filePath);
            var metadata = await JsonSerializer.DeserializeAsync<GameMetadata>(stream, JsonOptions);

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
        var filename = FileId.TryDecode(id);
        if (filename is null)
            return null;

        if (!File.Exists(filename))
            return null;

        var fileInfo = new FileInfo(filename);
        var readUrl = $"{_gameServiceOptions.BaseUrl}/{FileId.Encode(filename)}/game";

        await using var fileStream = File.OpenRead(filename);
        var metadata = await JsonSerializer.DeserializeAsync<GameMetadata>(fileStream, JsonOptions);

        if (metadata is null)
            return null;

        return new GameDto(
            id,
            metadata.Title,
            metadata.Platform,
            metadata.Description,
            metadata.Year,
            fileInfo.Length,
            readUrl
        );
    }

    public Task<Stream?> OpenReadAsync(string id)
    {
        var filename = FileId.TryDecode(id);
        if (filename is null)
            return Task.FromResult<Stream?>(null);

        if (!File.Exists(filename))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Task.FromResult<Stream?>(stream);
    }
}
