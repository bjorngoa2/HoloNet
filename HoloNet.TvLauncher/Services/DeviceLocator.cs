using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Logging;
using Windows.Devices.Geolocation;

namespace HoloNet.TvLauncher.Services;

public interface IDeviceLocator
{
    /// <summary>
    /// This PC's position, or <c>null</c> when Windows can't tell (location turned off,
    /// access denied, or no fix in time).
    /// </summary>
    Task<GeoCoordinate?> GetCoordinateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Asks the Windows location service, which on a PC without GPS positions it from nearby
/// Wi-Fi networks (typically to within a few hundred metres). Needs "Location services" and
/// "Let desktop apps access your location" turned on in Windows Settings → Privacy.
/// </summary>
public class WindowsDeviceLocator(ILogger<WindowsDeviceLocator> logger) : IDeviceLocator
{
    // A TV doesn't move, so an hour-old fix is as good as a fresh one and saves a Wi-Fi scan.
    private static readonly TimeSpan MaximumAge = TimeSpan.FromHours(1);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public async Task<GeoCoordinate?> GetCoordinateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await Geolocator.RequestAccessAsync().AsTask(cancellationToken);
            if (access != GeolocationAccessStatus.Allowed)
            {
                logger.LogInformation("Windows location access is {Access}; the TV's location is unknown", access);
                return null;
            }

            var position = await new Geolocator()
                .GetGeopositionAsync(MaximumAge, Timeout)
                .AsTask(cancellationToken);
            var point = position.Coordinate.Point.Position;
            return new GeoCoordinate(point.Latitude, point.Longitude);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Windows reports a disabled location service or a timed-out fix as COM exceptions.
            logger.LogWarning(ex, "Couldn't get this PC's location from Windows");
            return null;
        }
    }
}
