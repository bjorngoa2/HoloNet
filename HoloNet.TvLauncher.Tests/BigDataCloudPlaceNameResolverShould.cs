using System.Globalization;
using System.Net;
using System.Net.Http;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Tests;

public class BigDataCloudPlaceNameResolverShould
{
    [Theory]
    [InlineData("""{"city":"Stavanger","locality":"Madla"}""", "Stavanger")]
    [InlineData("""{"city":"","locality":"Sola"}""", "Sola")]
    [InlineData("""{"city":"","locality":""}""", null)]
    public async Task NameTheCity_OrElseTheLocality(string body, string? expected) =>
        Assert.Equal(expected, await CreateResolver(new StubHttpHandler(HttpStatusCode.OK, body)).GetNameAsync(new GeoCoordinate(58.99, 5.68)));

    [Fact]
    public async Task AskInTheUiLanguage_WithCoordinatesInInvariantFormat()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """{"city":"Stavanger"}""");
        var (previousCulture, previousUiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nb-NO");
        try
        {
            await CreateResolver(handler).GetNameAsync(new GeoCoordinate(58.99, 5.68));

            Assert.Contains("latitude=58.99&longitude=5.68&localityLanguage=nb", handler.RequestedUrl);
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (previousCulture, previousUiCulture);
        }
    }

    [Fact]
    public async Task ReturnNull_WhenTheLookUpFails() =>
        Assert.Null(await CreateResolver(new StubHttpHandler(HttpStatusCode.TooManyRequests, "")).GetNameAsync(new GeoCoordinate(58.99, 5.68)));

    private static BigDataCloudPlaceNameResolver CreateResolver(StubHttpHandler handler) => new(
        new HttpClient(handler),
        Options.Create(new TvLauncherOptions { ReverseGeocodingApiBaseUrl = "https://geocode.example.test/reverse" }),
        NullLogger<BigDataCloudPlaceNameResolver>.Instance);
}
