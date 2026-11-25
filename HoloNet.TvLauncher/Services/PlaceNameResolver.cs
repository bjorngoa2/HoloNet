using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public interface IPlaceNameResolver
{
    /// <summary>
    /// The name of the town or city at <paramref name="coordinate"/>, in the UI language, or
    /// <c>null</c> when it can't be looked up.
    /// </summary>
    Task<string?> GetNameAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Reverse-geocodes with BigDataCloud's free client-side endpoint (see
/// <see cref="TvLauncherOptions.ReverseGeocodingApiBaseUrl"/>), which needs no API key.
/// </summary>
public class BigDataCloudPlaceNameResolver(
    HttpClient httpClient,
    IOptions<TvLauncherOptions> options,
    ILogger<BigDataCloudPlaceNameResolver> logger) : IPlaceNameResolver
{
    private readonly TvLauncherOptions _options = options.Value;

    public async Task<string?> GetNameAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ReverseGeocodingApiBaseUrl))
            return null;

        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"{_options.ReverseGeocodingApiBaseUrl}?latitude={coordinate.Latitude}&longitude={coordinate.Longitude}&localityLanguage={CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}");

        try
        {
            var response = await httpClient.GetFromJsonAsync<ReverseGeocodeResponse>(url, cancellationToken);
            var name = new[] { response?.City, response?.Locality }.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
            return name?.Trim();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException
                                   && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Couldn't look up the place name from {Url}", url);
            return null;
        }
    }

    private sealed record ReverseGeocodeResponse(string? City, string? Locality);
}
