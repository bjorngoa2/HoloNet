using System.Text.Json;
using HoloNet.Games.Configuration;
using HoloNet.Games.Models;
using HoloNet.Shared.Helpers;
using Microsoft.Extensions.Options;

namespace HoloNet.Games.Services;

public interface IGameService
{
    Task<IEnumerable<GameDto>> GetAllAsync(string? platform = null, int? year = null, string? genre = null);
    Task<GameDto?> GetAsync(string id);
    Task<LaunchIntentDto?> GetLaunchIntentAsync(string id);
}

public class GameService(IOptions<GameServiceOptions> options) : IGameService
{
    private readonly GameServiceOptions _gameServiceOptions = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IEnumerable<GameDto>> GetAllAsync(string? platform = null, int? year = null, string? genre = null)
    {
        var directory = _gameServiceOptions.GetGameDirectory();

        // Directory.EnumerateFiles has no async equivalent; offload the (potentially slow,
        // e.g. network share) scan to a background thread so it doesn't block the request thread.
        var files = await Task.Run(() => Directory
            .EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories)
            .Where(x => string.Equals(Path.GetExtension(x), ".json", StringComparison.OrdinalIgnoreCase))
            .ToList());

        List<GameDto> games = [];
        foreach (var filePath in files)
        {
            var urlSafeId = FileId.Encode(filePath);

            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true);
            var metadata = await JsonSerializer.DeserializeAsync<GameMetadata>(stream, JsonOptions);

            if (metadata is null) continue;

            if (platform is not null && !string.Equals(metadata.Platform, platform, StringComparison.OrdinalIgnoreCase))
                continue;

            if (year is not null && metadata.Year != year)
                continue;

            if (genre is not null &&
                (metadata.Genre is null || !metadata.Genre.Any(g => string.Equals(g, genre, StringComparison.OrdinalIgnoreCase))))
                continue;

            var gameFile = FindGameFile(filePath);

            games.Add(new GameDto(
                urlSafeId,
                metadata.Title,
                metadata.Platform,
                metadata.Description,
                metadata.Year,
                metadata.Genre,
                gameFile is null ? null : _gameServiceOptions.GetNetworkPath(gameFile),
                gameFile is null ? null : new FileInfo(gameFile).Length
            ));
        }

        return games;
    }

    public async Task<GameDto?> GetAsync(string id)
    {
        var filename = FileId.TryDecode(id);
        if (filename is null || !_gameServiceOptions.GetGameDirectory().Contains(filename))
            return null;

        if (!File.Exists(filename))
            return null;

        await using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        var metadata = await JsonSerializer.DeserializeAsync<GameMetadata>(fileStream, JsonOptions);

        if (metadata is null)
            return null;

        var gameFile = FindGameFile(filename);

        return new GameDto(
            id,
            metadata.Title,
            metadata.Platform,
            metadata.Description,
            metadata.Year,
            metadata.Genre,
            gameFile is null ? null : _gameServiceOptions.GetNetworkPath(gameFile),
            gameFile is null ? null : new FileInfo(gameFile).Length
        );
    }

    public async Task<LaunchIntentDto?> GetLaunchIntentAsync(string id)
    {
        var game = await GetAsync(id);
        if (game is null || game.NetworkPath is null)
            return null;

        return new LaunchIntentDto(game.Id, game.Title, game.Platform, game.NetworkPath);
    }

    /// <summary>
    /// Finds the game file (ISO/CHD/etc.) sitting alongside a metadata.json sidecar.
    /// </summary>
    private static string? FindGameFile(string metadataFilePath)
    {
        var directory = Path.GetDirectoryName(metadataFilePath);
        if (directory is null)
            return null;

        return Directory.EnumerateFiles(directory)
            .FirstOrDefault(x => !string.Equals(Path.GetExtension(x), ".json", StringComparison.OrdinalIgnoreCase));
    }
}
