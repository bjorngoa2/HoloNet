using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Tests;

public class HomeLocationServiceShould
{
    private static readonly GeoCoordinate Sola = new(58.98712, 5.68349);

    [Fact]
    public async Task UseTheConfiguredPlace_WithoutAskingWindows()
    {
        var locator = new FakeLocator(Sola);
        var service = CreateService(
            new TvLauncherOptions { WeatherLatitude = 59.91, WeatherLongitude = 10.75, WeatherLocationName = " Oslo " },
            locator,
            new FakeResolver("Stavanger"));

        Assert.Equal(new Place(new GeoCoordinate(59.91, 10.75), "Oslo"), await service.GetAsync());
        Assert.Equal(0, locator.Calls);
    }

    [Fact]
    public async Task NameThisPcsLocation_RoundedToAboutAKilometre()
    {
        var resolver = new FakeResolver("Stavanger");
        var service = CreateService(new TvLauncherOptions(), new FakeLocator(Sola), resolver);

        Assert.Equal(new Place(new GeoCoordinate(58.99, 5.68), "Stavanger"), await service.GetAsync());
        Assert.Equal([new GeoCoordinate(58.99, 5.68)], resolver.Requests);
    }

    [Fact]
    public async Task LookUpEachPlaceNameOnce()
    {
        var resolver = new FakeResolver("Stavanger");
        var service = CreateService(new TvLauncherOptions(), new FakeLocator(Sola), resolver);

        await service.GetAsync();
        await service.GetAsync();

        Assert.Single(resolver.Requests);
    }

    [Fact]
    public async Task RetryTheNameLookUp_WhenItFailed()
    {
        var resolver = new FakeResolver(null);
        var service = CreateService(new TvLauncherOptions(), new FakeLocator(Sola), resolver);

        Assert.Equal("", (await service.GetAsync())?.Name);
        resolver.Name = "Stavanger";
        Assert.Equal("Stavanger", (await service.GetAsync())?.Name);
    }

    [Fact]
    public async Task PreferTheConfiguredName_OverTheLookedUpOne()
    {
        var resolver = new FakeResolver("Stavanger");
        var service = CreateService(new TvLauncherOptions { WeatherLocationName = "Hjemme" }, new FakeLocator(Sola), resolver);

        Assert.Equal("Hjemme", (await service.GetAsync())?.Name);
        Assert.Empty(resolver.Requests);
    }

    [Fact]
    public async Task ReturnNull_WhenWindowsDoesNotKnowTheLocation() =>
        Assert.Null(await CreateService(new TvLauncherOptions(), new FakeLocator(null), new FakeResolver("Stavanger")).GetAsync());

    private static HomeLocationService CreateService(TvLauncherOptions options, IDeviceLocator locator, IPlaceNameResolver resolver) =>
        new(Options.Create(options), locator, resolver);

    private sealed class FakeLocator(GeoCoordinate? coordinate) : IDeviceLocator
    {
        public int Calls { get; private set; }

        public Task<GeoCoordinate?> GetCoordinateAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(coordinate);
        }
    }

    private sealed class FakeResolver(string? name) : IPlaceNameResolver
    {
        public string? Name { get; set; } = name;

        public List<GeoCoordinate> Requests { get; } = [];

        public Task<string?> GetNameAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default)
        {
            Requests.Add(coordinate);
            return Task.FromResult(Name);
        }
    }
}
