using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

/// <summary>The ambient clock screensaver (see <see cref="ScreensaverStyle.AmbientClock"/>).</summary>
public partial class AmbientClockScene : UserControl, IScreensaverScene
{
    private const double SafeMargin = 120;
    private static readonly TimeSpan MoveInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan WeatherRefreshInterval = TimeSpan.FromMinutes(30);

    // A report this old is dropped rather than shown as if it were current.
    private static readonly TimeSpan WeatherMaxAge = TimeSpan.FromHours(2);
    private static readonly Duration MoveFade = new(TimeSpan.FromSeconds(1.5));

    private readonly IWeatherService _weatherService;
    private readonly TimeProvider _timeProvider;
    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _moveTimer = new() { Interval = MoveInterval };
    private readonly Random _random = new();

    private WeatherReport? _weather;
    private DateTimeOffset _weatherReceivedAt = DateTimeOffset.MinValue;
    private DateTimeOffset _weatherCheckedAt = DateTimeOffset.MinValue;
    private bool _isFetchingWeather;

    public AmbientClockScene(IWeatherService weatherService, TimeProvider timeProvider)
    {
        _weatherService = weatherService;
        _timeProvider = timeProvider;
        InitializeComponent();

        _clockTimer.Tick += (_, _) => ShowTime();
        _moveTimer.Tick += (_, _) => MoveBlock();
    }

    public ScreensaverStyle ScreensaverStyle => ScreensaverStyle.AmbientClock;

    public FrameworkElement View => this;

    public void Start()
    {
        ShowTime();
        ShowWeather();
        PlaceBlock();
        Motion.FadeTo(Block, 1, MoveFade);

        _clockTimer.Start();
        _moveTimer.Start();
        _ = RefreshWeatherIfStaleAsync();
    }

    public void Stop()
    {
        _clockTimer.Stop();
        _moveTimer.Stop();
        Block.BeginAnimation(OpacityProperty, null);
        Block.Opacity = 0;
    }

    private void ShowTime()
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        TimeText.Text = AmbientClock.FormatTime(now, CultureInfo.CurrentCulture);
        DateText.Text = AmbientClock.FormatDate(now, CultureInfo.CurrentCulture);
    }

    private void ShowWeather()
    {
        if (_weather is null)
        {
            WeatherPanel.Visibility = Visibility.Collapsed;
            return;
        }

        WeatherGlyphText.Text = AmbientClock.WeatherGlyph(_weather.WeatherCode, _weather.IsDay);
        TemperatureText.Text = AmbientClock.FormatTemperature(_weather.TemperatureCelsius, CultureInfo.CurrentCulture);
        LocationText.Text = _weather.LocationName;
        LocationText.Visibility = string.IsNullOrEmpty(_weather.LocationName) ? Visibility.Collapsed : Visibility.Visible;
        WeatherPanel.Visibility = Visibility.Visible;
    }

    /// <summary>Fades the block out, moves it to a fresh spot, and fades it back in.</summary>
    private void MoveBlock()
    {
        _ = RefreshWeatherIfStaleAsync();
        Motion.FadeTo(Block, 0, MoveFade, (_, _) =>
        {
            if (!_moveTimer.IsEnabled)
                return;

            ShowWeather();
            PlaceBlock();
            Motion.FadeTo(Block, 1, MoveFade);
        });
    }

    private void PlaceBlock()
    {
        Block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var position = AmbientClock.NextPosition(new Size(Stage.Width, Stage.Height), Block.DesiredSize, SafeMargin, _random);
        Canvas.SetLeft(Block, position.X);
        Canvas.SetTop(Block, position.Y);
    }

    /// <summary>
    /// Fetches the weather when the last report is old. A new report is held back until the
    /// block next moves, so the layout never jumps while it's on screen, except for the very
    /// first report, which fills an empty weather row.
    /// </summary>
    private async Task RefreshWeatherIfStaleAsync()
    {
        if (_isFetchingWeather || _timeProvider.GetUtcNow() - _weatherCheckedAt < WeatherRefreshInterval)
            return;

        _isFetchingWeather = true;
        try
        {
            var wasEmpty = _weather is null;
            var report = await _weatherService.GetCurrentAsync();
            var now = _timeProvider.GetUtcNow();
            _weatherCheckedAt = now;

            if (report is not null)
                (_weather, _weatherReceivedAt) = (report, now);
            else if (now - _weatherReceivedAt > WeatherMaxAge)
                _weather = null;

            if (wasEmpty && _weather is not null && _clockTimer.IsEnabled)
                ShowWeather();
        }
        finally
        {
            _isFetchingWeather = false;
        }
    }
}
