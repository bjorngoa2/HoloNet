namespace HoloNet.TvLauncher.Models;

/// <summary>Where the TV is (see <see cref="Services.IHomeLocationService"/>).</summary>
/// <param name="Name">A short place name such as <c>Stavanger</c>, or empty when it isn't known.</param>
public sealed record Place(GeoCoordinate Coordinate, string Name);
