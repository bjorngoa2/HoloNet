using System.Globalization;
using System.Windows;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

public class AmbientClockShould
{
    private static readonly CultureInfo Norwegian = CultureInfo.GetCultureInfo("nb-NO");
    private static readonly DateTime MondayEvening = new(2025, 11, 24, 20, 7, 0);

    [Fact]
    public void FormatTheTimeAndDate_InTheCulturesOwnStyle()
    {
        Assert.Equal("20:07", AmbientClock.FormatTime(MondayEvening, Norwegian));
        Assert.Equal("mandag 24. november", AmbientClock.FormatDate(MondayEvening, Norwegian));
        Assert.Equal("Monday November 24", AmbientClock.FormatDate(MondayEvening, CultureInfo.GetCultureInfo("en-US")));
    }

    [Theory]
    [InlineData(-0.3, "0°")]
    [InlineData(-2.5, "-3°")]
    [InlineData(12.5, "13°")]
    public void RoundTemperaturesToWholeDegrees_WithoutANegativeZero(double celsius, string expected) =>
        Assert.Equal(expected, AmbientClock.FormatTemperature(celsius, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(0, true, "\u2600")]
    [InlineData(0, false, "\u263E")]
    [InlineData(63, true, "\U0001F327")]
    [InlineData(75, true, "\u2744")]
    [InlineData(1234, true, "\u2601")]
    public void PickAWeatherGlyph_ForAWmoWeatherCode(int weatherCode, bool isDay, string expected) =>
        Assert.Equal(expected, AmbientClock.WeatherGlyph(weatherCode, isDay));

    [Fact]
    public void KeepTheBlockInsideTheSafeArea_WhereverItMoves()
    {
        var area = new Size(1920, 1080);
        var block = new Size(600, 400);
        var random = new Random(42);

        for (var i = 0; i < 1000; i++)
        {
            var position = AmbientClock.NextPosition(area, block, margin: 120, random);
            Assert.InRange(position.X, 120, 1920 - 600 - 120);
            Assert.InRange(position.Y, 120, 1080 - 400 - 120);
        }
    }

    [Fact]
    public void CentreTheBlock_WhenItDoesNotFitInsideTheMargins() =>
        Assert.Equal(new Point(100, 0), AmbientClock.NextPosition(new Size(1000, 500), new Size(800, 500), margin: 120, new Random(1)));
}
