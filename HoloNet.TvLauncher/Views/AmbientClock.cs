using System.Globalization;
using System.Windows;

namespace HoloNet.TvLauncher.Views;

/// <summary>Text and placement for the ambient clock screensaver (see <see cref="AmbientClockScene"/>).</summary>
public static class AmbientClock
{
    /// <summary>The time in the culture's short format, e.g. "20:37" in Norway.</summary>
    public static string FormatTime(DateTime now, CultureInfo culture) => now.ToString("t", culture);

    /// <summary>
    /// The weekday and date without the year, in the culture's own order, e.g. "mandag 23.
    /// november" in Norway or "Monday November 23" in the US.
    /// </summary>
    public static string FormatDate(DateTime now, CultureInfo culture) =>
        $"{now.ToString("dddd", culture)} {now.ToString(culture.DateTimeFormat.MonthDayPattern, culture)}";

    /// <summary>
    /// Whole degrees, e.g. "-3°". Rounds before formatting, so -0.3 shows as "0°" instead of
    /// "-0°".
    /// </summary>
    public static string FormatTemperature(double celsius, CultureInfo culture) =>
        $"{((int)Math.Round(celsius, MidpointRounding.AwayFromZero)).ToString(culture)}°";

    /// <summary>
    /// A weather symbol from Segoe UI Symbol for a WMO weather interpretation code, as reported
    /// by Open-Meteo. Clear skies show a moon at night.
    /// </summary>
    public static string WeatherGlyph(int weatherCode, bool isDay) => weatherCode switch
    {
        0 => isDay ? "\u2600" : "\u263E",
        1 or 2 => isDay ? "\u26C5" : "\u2601",
        45 or 48 => "\U0001F32B",
        >= 51 and <= 67 or >= 80 and <= 82 => "\U0001F327",
        >= 71 and <= 77 or 85 or 86 => "\u2744",
        >= 95 and <= 99 => "\u26C8",
        _ => "\u2601"
    };

    /// <summary>
    /// A random top-left position for content of <paramref name="contentSize"/> that keeps it
    /// at least <paramref name="margin"/> inside <paramref name="area"/>, or centred when it
    /// doesn't fit. Moving to a fresh spot regularly keeps any one group of pixels from staying
    /// lit, which protects OLED TVs from burn-in.
    /// </summary>
    public static Point NextPosition(Size area, Size contentSize, double margin, Random random)
    {
        return new Point(Pick(area.Width, contentSize.Width), Pick(area.Height, contentSize.Height));

        double Pick(double available, double content)
        {
            var room = available - content - (2 * margin);
            return room <= 0
                ? Math.Max(0, (available - content) / 2)
                : margin + (random.NextDouble() * room);
        }
    }
}
