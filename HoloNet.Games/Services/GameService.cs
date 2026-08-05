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

            var networkPath = FindGameFileNetworkPath(filePath);

            games.Add(new GameDto(
                urlSafeId,
                metadata.Title,
                metadata.Platform,
                metadata.Description,
                metadata.Year,
                metadata.Genre,
                networkPath
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

        return new GameDto(
            id,
            metadata.Title,
            metadata.Platform,
            metadata.Description,
            metadata.Year,
            metadata.Genre,
            FindGameFileNetworkPath(filename)
        );
    }

    /// <summary>
    /// Finds the game file (ISO/CHD/etc.) sitting alongside a metadata.json sidecar and maps it to a
    /// UNC path under the configured network share, so emulators can open it directly instead of
    /// downloading it through the API.
    /// </summary>
    private string? FindGameFileNetworkPath(string metadataFilePath)
    {
        var directory = Path.GetDirectoryName(metadataFilePath);
        if (directory is null)
            return null;

        var gameFile = Directory.EnumerateFiles(directory)
            .FirstOrDefault(x => !string.Equals(Path.GetExtension(x), ".json", StringComparison.OrdinalIgnoreCase));

        return gameFile is null ? null : _gameServiceOptions.GetNetworkPath(gameFile);
    }
}
