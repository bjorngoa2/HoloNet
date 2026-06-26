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
}

public class GameService(IOptions<GameServiceOptions> options) : IGameService
{
    private readonly GameServiceOptions _gameServiceOptions = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IEnumerable<GameDto>> GetAllAsync()
    {
        var directory = _gameServiceOptions.GetGameDirectory();

        var files = Directory
            .EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories)
            .Where(x => string.Equals(Path.GetExtension(x), ".json", StringComparison.OrdinalIgnoreCase));

        List<GameDto> games = [];
        foreach (var filePath in files)
        {
            var urlSafeId = FileId.Encode(filePath);

            await using var stream = File.OpenRead(filePath);
            var metadata = await JsonSerializer.DeserializeAsync<GameMetadata>(stream, JsonOptions);

            if (metadata is null) continue;

            games.Add(new GameDto(
                urlSafeId,
                metadata.Title,
                metadata.Platform,
                metadata.Description,
                metadata.Year,
                metadata.Genre
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
            metadata.Genre
        );
    }
}
