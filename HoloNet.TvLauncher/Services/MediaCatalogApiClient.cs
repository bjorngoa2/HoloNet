using System.Net.Http;
using System.Net.Http.Json;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public interface IMediaCatalogApiClient
{
    /// <summary>
    /// Fetches one title, with its seasons, episodes and watch progress, from HoloNet.Video's
    /// <c>GET api/v1/catalog/{provider}/{id}</c>. Returns <c>null</c> when the title can't be
    /// fetched (e.g. an expired Prime session cookie, or a title the service has dropped), so
    /// one stale library entry doesn't blank out the rest of a folder.
    /// </summary>
    /// <param name="provider">The catalog provider route segment, e.g. <c>PrimeVideo</c>.</param>
    Task<MediaItemDto?> GetItemAsync(string provider, string titleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Thin facade over HoloNet.Video's <c>api/v1/catalog</c> endpoints, mirroring
/// <see cref="GamesApiClient"/>'s shape.
/// </summary>
public class MediaCatalogApiClient(HttpClient httpClient, IOptions<TvLauncherOptions> options) : IMediaCatalogApiClient
{
    private readonly string _baseUrl = options.Value.VideoCatalogApiBaseUrl.TrimEnd('/');

    public async Task<MediaItemDto?> GetItemAsync(string provider, string titleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
            return null;

        try
        {
            using var response = await httpClient.GetAsync(
                $"{_baseUrl}/{Uri.EscapeDataString(provider)}/{Uri.EscapeDataString(titleId)}", cancellationToken);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<MediaItemDto>(cancellationToken)
                : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}