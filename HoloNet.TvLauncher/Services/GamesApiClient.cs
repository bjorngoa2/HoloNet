using System.Net.Http;
using System.Net.Http.Json;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public interface IGamesApiClient
{
    Task<IReadOnlyList<GameDto>> GetGamesAsync(CancellationToken cancellationToken = default);

    Task<LaunchIntentDto?> GetLaunchIntentAsync(string gameId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Thin facade over the HoloNet.Games HTTP API. Hides the base-URL composition and JSON
/// deserialization from the UI/launcher logic.
/// </summary>
public class GamesApiClient : IGamesApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public GamesApiClient(HttpClient httpClient, IOptions<TvLauncherOptions> options)
    {
        _httpClient = httpClient;
        _baseUrl = options.Value.GamesApiBaseUrl.TrimEnd('/');
    }

    public async Task<IReadOnlyList<GameDto>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        var games = await _httpClient.GetFromJsonAsync<List<GameDto>>(_baseUrl, cancellationToken);
        return games ?? [];
    }

    public async Task<LaunchIntentDto?> GetLaunchIntentAsync(string gameId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/{gameId}/launch", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<LaunchIntentDto>(cancellationToken);
    }
}
