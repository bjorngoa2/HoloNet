using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HoloNet.TvLauncher.Models;

namespace HoloNet.TvLauncher.Views;

/// <summary>The bouncing logo screensaver (see <see cref="ScreensaverStyle.BouncingLogo"/>).</summary>
public partial class BouncingLogoScene : UserControl, IScreensaverScene
{
    // Often enough to look smooth, without a storyboard for something this simple.
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(30);
    private const double SpeedPixelsPerSecond = 40;

    private readonly DispatcherTimer _frameTimer = new() { Interval = FrameInterval };
    private double _x;
    private double _y;
    private double _velocityX;
    private double _velocityY;

    public BouncingLogoScene()
    {
        InitializeComponent();
        _frameTimer.Tick += (_, _) => MoveLogo();
    }

    public ScreensaverStyle ScreensaverStyle => ScreensaverStyle.BouncingLogo;

    public FrameworkElement View => this;

    public void Start()
    {
        (_x, _y) = (0, 0);
        (_velocityX, _velocityY) = (SpeedPixelsPerSecond, SpeedPixelsPerSecond);
        MoveLogo();
        _frameTimer.Start();
    }

    public void Stop() => _frameTimer.Stop();

    private void MoveLogo()
    {
        var maxX = Math.Max(0, ActualWidth - Logo.ActualWidth);
        var maxY = Math.Max(0, ActualHeight - Logo.ActualHeight);
        var seconds = FrameInterval.TotalSeconds;

        _x += _velocityX * seconds;
        _y += _velocityY * seconds;

        if (_x <= 0 || _x >= maxX)
        {
            _x = Math.Clamp(_x, 0, maxX);
            _velocityX = -_velocityX;
        }

        if (_y <= 0 || _y >= maxY)
        {
            _y = Math.Clamp(_y, 0, maxY);
            _velocityY = -_velocityY;
        }

        LogoTransform.X = _x;
        LogoTransform.Y = _y;
    }
}
