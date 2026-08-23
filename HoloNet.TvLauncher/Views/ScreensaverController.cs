using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using HoloNet.TvLauncher.Configuration;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Owns the idle-timeout burn-in-protection screensaver: watches for player inactivity (see
/// <see cref="NotifyActivity"/>) and, once idle long enough, shows a drifting "DVD logo" over
/// the given overlay elements until the player provides input again (see <see cref="Dismiss"/>).
/// Extracted out of <see cref="MainWindow"/> so the idle-timer/animation state and logic aren't
/// mixed in with navigation (see <see cref="FolderNavigator"/>) and launch-workflow concerns.
/// </summary>
public sealed class ScreensaverController : IDisposable
{
    // How often the screensaver logo's position is updated — a smooth-looking "DVD logo" drift
    // without needing a full WPF storyboard/animation for something this simple.
    private static readonly TimeSpan AnimationInterval = TimeSpan.FromMilliseconds(30);
    private const double LogoSpeedPixelsPerSecond = 40;

    private readonly TvLauncherOptions _options;
    private readonly FrameworkElement _overlay;
    private readonly FrameworkElement _logo;
    private readonly TranslateTransform _logoTransform;
    private readonly Func<double> _getViewportWidth;
    private readonly Func<double> _getViewportHeight;
    private readonly Func<bool> _isSuppressed;

    private readonly DispatcherTimer _idleCheckTimer;
    private DispatcherTimer? _animationTimer;

    private DateTime _lastActivityUtc = DateTime.UtcNow;
    private double _logoX;
    private double _logoY;
    private double _logoVelocityX = LogoSpeedPixelsPerSecond;
    private double _logoVelocityY = LogoSpeedPixelsPerSecond;

    public bool IsActive { get; private set; }

    /// <param name="isSuppressed">
    /// Checked before showing the screensaver, e.g. so it doesn't take over while an emulator
    /// has focus (already covering the window and constantly redrawing, so there's no burn-in
    /// risk from TvLauncher itself) or while a modal overlay/error message is up.
    /// </param>
    public ScreensaverController(
        TvLauncherOptions options,
        FrameworkElement overlay,
        FrameworkElement logo,
        TranslateTransform logoTransform,
        Func<double> getViewportWidth,
        Func<double> getViewportHeight,
        Func<bool> isSuppressed)
    {
        _options = options;
        _overlay = overlay;
        _logo = logo;
        _logoTransform = logoTransform;
        _getViewportWidth = getViewportWidth;
        _getViewportHeight = getViewportHeight;
        _isSuppressed = isSuppressed;

        _idleCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleCheckTimer.Tick += (_, _) => CheckIdleTimeout();
    }

    /// <summary>
    /// Starts the idle-check timer, a no-op if <see cref="TvLauncherOptions.ScreensaverEnabled"/>
    /// is <c>false</c>.
    /// </summary>
    public void Start()
    {
        if (_options.ScreensaverEnabled)
            _idleCheckTimer.Start();
    }

    /// <summary>Resets the idle clock in response to player input.</summary>
    public void NotifyActivity() => _lastActivityUtc = DateTime.UtcNow;

    /// <summary>Hides the screensaver. Only valid to call while <see cref="IsActive"/>.</summary>
    public void Dismiss() => Hide();

    private void CheckIdleTimeout()
    {
        if (IsActive || _isSuppressed())
            return;

        var idleFor = DateTime.UtcNow - _lastActivityUtc;
        if (idleFor.TotalMinutes >= _options.ScreensaverIdleMinutes)
            Show();
    }

    private void Show()
    {
        IsActive = true;
        _overlay.Visibility = Visibility.Visible;

        _logoX = 0;
        _logoY = 0;
        _logoVelocityX = LogoSpeedPixelsPerSecond;
        _logoVelocityY = LogoSpeedPixelsPerSecond;

        _animationTimer ??= new DispatcherTimer { Interval = AnimationInterval };
        _animationTimer.Tick -= OnAnimationTick;
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    private void Hide()
    {
        IsActive = false;
        _overlay.Visibility = Visibility.Collapsed;
        _animationTimer?.Stop();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        var maxX = Math.Max(0, _getViewportWidth() - _logo.ActualWidth);
        var maxY = Math.Max(0, _getViewportHeight() - _logo.ActualHeight);
        var deltaSeconds = AnimationInterval.TotalSeconds;

        _logoX += _logoVelocityX * deltaSeconds;
        _logoY += _logoVelocityY * deltaSeconds;

        if (_logoX <= 0 || _logoX >= maxX)
        {
            _logoX = Math.Clamp(_logoX, 0, maxX);
            _logoVelocityX = -_logoVelocityX;
        }

        if (_logoY <= 0 || _logoY >= maxY)
        {
            _logoY = Math.Clamp(_logoY, 0, maxY);
            _logoVelocityY = -_logoVelocityY;
        }

        _logoTransform.X = _logoX;
        _logoTransform.Y = _logoY;
    }

    public void Dispose()
    {
        _idleCheckTimer.Stop();
        _animationTimer?.Stop();
    }
}
