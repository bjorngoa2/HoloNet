using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public interface IWeatherService
{
    /// <summary>
    /// The current weather where the TV is (see <see cref="IHomeLocationService"/>), or
    /// <c>null</c> when the location is unknown or the weather can't be fetched. The weather
    /// is a nice-to-have, so callers just leave it out rather than reporting an error.
    /// </summary>
    Task<WeatherReport?> GetCurrentAsync(CancellationToken cancellationToken = default);
}

/// <summary>Reads the current weather from Open-Meteo (see <see cref="TvLauncherOptions.WeatherApiBaseUrl"/>).</summary>
public class OpenMeteoWeatherService(
    HttpClient httpClient,
    IHomeLocationService homeLocation,
    IOptions<TvLauncherOptions> options,
    ILogger<OpenMeteoWeatherService> logger) : IWeatherService
{
    private readonly TvLauncherOptions _options = options.Value;

    public async Task<WeatherReport?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WeatherApiBaseUrl)
            || await homeLocation.GetAsync(cancellationToken) is not { } place)
            return null;

        // Invariant formatting: under a Norwegian culture 59.91 would become "59,91".
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"{_options.WeatherApiBaseUrl}?latitude={place.Coordinate.Latitude}&longitude={place.Coordinate.Longitude}&current=temperature_2m,weather_code,is_day");

        try
        {
            var response = await httpClient.GetFromJsonAsync<ForecastResponse>(url, cancellationToken);
            return response?.Current is { } current
                ? new WeatherReport(current.Temperature, current.WeatherCode, current.IsDay == 1, place.Name)
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException
                                   && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Couldn't fetch the weather from {Url}", url);
            return null;
        }
    }

    private sealed record ForecastResponse(CurrentWeather? Current);

    private sealed record CurrentWeather(
        [property: JsonPropertyName("temperature_2m")] double Temperature,
        [property: JsonPropertyName("weather_code")] int WeatherCode,
        [property: JsonPropertyName("is_day")] int IsDay);
}
