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
    Task<Stream?> OpenThumbnailReadAsync(string id);
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

            games.Add(BuildGameDto(urlSafeId, filePath, metadata));
        }

        return games;
    }

    /// <summary>
    /// Resolves a game id to its metadata.json path, or <c>null</c> if the id is malformed or
    /// doesn't decode to a path within <see cref="GameServiceOptions.GetGameDirectory"/> — the
    /// path-traversal guard shared by every endpoint that accepts a game id.
    /// </summary>
    private string? ResolveMetadataPath(string id)
    {
        var filename = FileId.TryDecode(id);
        return filename is not null && _gameServiceOptions.GetGameDirectory().Contains(filename)
            ? filename
            : null;
    }

    public async Task<GameDto?> GetAsync(string id)
    {
        var filename = ResolveMetadataPath(id);
        if (filename is null || !File.Exists(filename))
            return null;

        await using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        var metadata = await JsonSerializer.DeserializeAsync<GameMetadata>(fileStream, JsonOptions);

        return metadata is null ? null : BuildGameDto(id, filename, metadata);
    }

    /// <summary>
    /// Builds the API-facing <see cref="GameDto"/> for a game whose metadata.json has already
    /// been decoded, resolving its sibling game/thumbnail files (see <see cref="FindGameFile"/>
    /// and <see cref="FindThumbnailFile"/>) along the way. Shared by <see cref="GetAllAsync"/>
    /// and <see cref="GetAsync"/> so the mapping from metadata to DTO can't drift between them.
    /// </summary>
    private GameDto BuildGameDto(string id, string metadataFilePath, GameMetadata metadata)
    {
        var gameFile = FindGameFile(metadataFilePath);
        var thumbnailFile = FindThumbnailFile(metadataFilePath);

        return new GameDto(
            id,
            metadata.Title,
            metadata.Platform,
            metadata.Description,
            metadata.Year,
            metadata.Genre,
            gameFile is null ? null : _gameServiceOptions.GetNetworkPath(gameFile),
            gameFile is null ? null : new FileInfo(gameFile).Length,
            thumbnailFile is null ? null : $"{_gameServiceOptions.GetBaseUrl()}/{id}/thumbnail"
        );
    }

    public async Task<LaunchIntentDto?> GetLaunchIntentAsync(string id)
    {
        var game = await GetAsync(id);
        if (game is null || game.NetworkPath is null)
            return null;

        return new LaunchIntentDto(game.Id, game.Title, game.Platform, game.NetworkPath);
    }

    public Task<Stream?> OpenThumbnailReadAsync(string id)
    {
        var metadataFilePath = ResolveMetadataPath(id);
        if (metadataFilePath is null)
            return Task.FromResult<Stream?>(null);

        var thumbnailFile = FindThumbnailFile(metadataFilePath);
        if (thumbnailFile is null || !File.Exists(thumbnailFile))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(thumbnailFile, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    /// <summary>
    /// Finds the game file (ISO/CHD/etc.) sitting alongside a metadata.json sidecar.
    /// </summary>
    private static string? FindGameFile(string metadataFilePath) =>
        FindSiblingFile(metadataFilePath, x => !string.Equals(Path.GetExtension(x), ".json", StringComparison.OrdinalIgnoreCase)
            && !ThumbnailFormat.IsThumbnail(x));

    /// <summary>
    /// Finds a cover-art/thumbnail image (jpg/png/webp) sitting alongside a metadata.json sidecar,
    /// if one exists, to be served via the <c>{id}/thumbnail</c> endpoint.
    /// </summary>
    private static string? FindThumbnailFile(string metadataFilePath) =>
        FindSiblingFile(metadataFilePath, ThumbnailFormat.IsThumbnail);

    /// <summary>
    /// Finds the first file in <paramref name="metadataFilePath"/>'s directory matching
    /// <paramref name="predicate"/>, or <c>null</c> if the metadata file has no parent directory
    /// (shouldn't happen for a real file path) or no file matches.
    /// </summary>
    private static string? FindSiblingFile(string metadataFilePath, Func<string, bool> predicate)
    {
        var directory = Path.GetDirectoryName(metadataFilePath);
        return directory is null ? null : Directory.EnumerateFiles(directory).FirstOrDefault(predicate);
    }
}
