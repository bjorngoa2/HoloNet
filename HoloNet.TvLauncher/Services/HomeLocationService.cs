using System.Collections.Concurrent;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public interface IHomeLocationService
{
    /// <summary>
    /// Where the TV is: the configured <see cref="TvLauncherOptions.WeatherLatitude"/> and
    /// <see cref="TvLauncherOptions.WeatherLongitude"/> when set, otherwise this PC's own
    /// location. <c>null</c> when neither is available.
    /// </summary>
    Task<Place?> GetAsync(CancellationToken cancellationToken = default);
}

public class HomeLocationService(
    IOptions<TvLauncherOptions> options,
    IDeviceLocator deviceLocator,
    IPlaceNameResolver placeNameResolver) : IHomeLocationService
{
    // Two decimals is about a kilometre: plenty for the weather, and the exact position of
    // the home never leaves the PC.
    private const int CoordinateDecimals = 2;

    private readonly TvLauncherOptions _options = options.Value;
    private readonly ConcurrentDictionary<GeoCoordinate, string> _names = new();

    public async Task<Place?> GetAsync(CancellationToken cancellationToken = default)
    {
        var configuredName = _options.WeatherLocationName.Trim();
        if (_options is { WeatherLatitude: { } latitude, WeatherLongitude: { } longitude })
            return new Place(new GeoCoordinate(latitude, longitude), configuredName);

        if (await deviceLocator.GetCoordinateAsync(cancellationToken) is not { } detected)
            return null;

        var coordinate = new GeoCoordinate(
            Math.Round(detected.Latitude, CoordinateDecimals),
            Math.Round(detected.Longitude, CoordinateDecimals));
        var name = configuredName.Length > 0 ? configuredName : await GetNameAsync(coordinate, cancellationToken);
        return new Place(coordinate, name);
    }

    // Only successful look-ups are remembered, so a failed one is retried next time.
    private async Task<string> GetNameAsync(GeoCoordinate coordinate, CancellationToken cancellationToken)
    {
        if (_names.TryGetValue(coordinate, out var cached))
            return cached;

        var name = await placeNameResolver.GetNameAsync(coordinate, cancellationToken);
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        _names[coordinate] = name;
        return name;
    }
}
