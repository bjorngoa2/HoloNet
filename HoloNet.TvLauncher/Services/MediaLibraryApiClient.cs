using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public interface IMediaLibraryApiClient
{
    /// <summary>
    /// The titles in one streaming folder, newest first, from HoloNet.Video's
    /// <c>GET api/v1/library/{provider}</c>. Throws <see cref="HttpRequestException"/> when the
    /// library can't be reached, so a refresh can keep showing what it already has.
    /// </summary>
    /// <param name="provider">The provider route segment, e.g. <c>PrimeVideo</c>.</param>
    Task<IReadOnlyList<LibraryTitleDto>> GetTitlesAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the title behind a Prime Video, Disney+ or HBO Max link (or bare id) to the library. The
    /// service is worked out from the link itself, so the title may land in a different folder
    /// than the one it was added from.
    /// </summary>
    Task<LibraryImportResult> ImportAsync(string urlOrId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a title from the library. Returns <c>false</c> only when the library couldn't
    /// be reached; a title that's already gone counts as removed.
    /// </summary>
    Task<bool> RemoveAsync(string provider, string titleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Thin facade over HoloNet.Video's <c>api/v1/library</c> endpoints, mirroring
/// <see cref="MediaCatalogApiClient"/>'s shape.
/// </summary>
public class MediaLibraryApiClient(HttpClient httpClient, IOptions<TvLauncherOptions> options) : IMediaLibraryApiClient
{
    private const string UnreachableMessage = "Couldn't reach the HoloNet video library. Check that it's running, then try again.";

    private readonly string _baseUrl = options.Value.VideoLibraryApiBaseUrl.TrimEnd('/');

    public async Task<IReadOnlyList<LibraryTitleDto>> GetTitlesAsync(string provider, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
            return [];

        var titles = await httpClient.GetFromJsonAsync<List<LibraryTitleDto>>(
            $"{_baseUrl}/{Uri.EscapeDataString(provider)}", cancellationToken);
        return titles ?? [];
    }

    public async Task<LibraryImportResult> ImportAsync(string urlOrId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
            return LibraryImportResult.Failed("TvLauncher:VideoLibraryApiBaseUrl isn't configured.");

        try
        {
            using var response = await httpClient.PostAsJsonAsync(_baseUrl, new ImportRequest(urlOrId), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var title = await response.Content.ReadFromJsonAsync<LibraryTitleDto>(cancellationToken);
                return title is null ? LibraryImportResult.Failed(UnreachableMessage) : LibraryImportResult.Added(title);
            }

            var problem = await ReadProblemAsync(response, cancellationToken);
            return LibraryImportResult.Failed(problem?.Detail ?? UnreachableMessage);
        }
        catch (HttpRequestException)
        {
            return LibraryImportResult.Failed(UnreachableMessage);
        }
    }

    public async Task<bool> RemoveAsync(string provider, string titleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
            return false;

        try
        {
            using var response = await httpClient.DeleteAsync(
                $"{_baseUrl}/{Uri.EscapeDataString(provider)}/{Uri.EscapeDataString(titleId)}", cancellationToken);
            return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    /// <summary>The RFC 9457 problem details HoloNet.Video returns for a rejected import.</summary>
    private static async Task<ProblemDetailsDto?> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private sealed record ImportRequest(string UrlOrId);

    private sealed record ProblemDetailsDto(string? Detail);
}