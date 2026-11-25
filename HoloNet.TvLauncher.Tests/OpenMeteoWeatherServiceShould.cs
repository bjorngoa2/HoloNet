using System.Globalization;
using System.Net;
using System.Net.Http;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Tests;

public class OpenMeteoWeatherServiceShould
{
    [Fact]
    public async Task ReadTheCurrentWeather_WithCoordinatesInInvariantFormat()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK,
            """{"current":{"time":"2025-11-24T20:00","interval":900,"temperature_2m":-1.4,"weather_code":71,"is_day":0}}""");
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
        try
        {
            var report = await CreateService(handler).GetCurrentAsync();

            Assert.Equal(new WeatherReport(-1.4, 71, IsDay: false, "Oslo"), report);
            Assert.Contains("latitude=59.91&longitude=10.75", handler.RequestedUrl);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public async Task ReturnNull_WithoutCallingOut_WhenTheLocationIsUnknown()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "{}");

        Assert.Null(await CreateService(handler, place: null).GetCurrentAsync());
        Assert.Null(handler.RequestedUrl);
    }

    [Fact]
    public async Task ReturnNull_WhenTheWeatherServiceFails() =>
        Assert.Null(await CreateService(new StubHttpHandler(HttpStatusCode.InternalServerError, "")).GetCurrentAsync());

    private static readonly Place Oslo = new(new GeoCoordinate(59.91, 10.75), "Oslo");

    private static OpenMeteoWeatherService CreateService(StubHttpHandler handler) => CreateService(handler, Oslo);

    private static OpenMeteoWeatherService CreateService(StubHttpHandler handler, Place? place) => new(
        new HttpClient(handler),
        new FixedHomeLocation(place),
        Options.Create(new TvLauncherOptions { WeatherApiBaseUrl = "https://weather.example.test/v1/forecast" }),
        NullLogger<OpenMeteoWeatherService>.Instance);

    private sealed class FixedHomeLocation(Place? place) : IHomeLocationService
    {
        public Task<Place?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(place);
    }
}
