namespace HoloNet.TvLauncher.Models;

/// <summary>The current weather where the TV is (see <see cref="Services.IWeatherService"/>).</summary>
/// <param name="WeatherCode">A WMO weather interpretation code, e.g. <c>3</c> for overcast.</param>
/// <param name="IsDay">Whether the sun is up, so clear skies can show a sun or a moon.</param>
public sealed record WeatherReport(double TemperatureCelsius, int WeatherCode, bool IsDay, string LocationName);
