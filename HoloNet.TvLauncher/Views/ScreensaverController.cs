using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Owns the idle-timeout burn-in-protection screensaver: watches for player inactivity (see
/// <see cref="NotifyActivity"/>) and, once idle long enough, covers the launcher with the scene
/// chosen in Settings (see <see cref="IScreensaverScene"/>) until the player provides input again
/// (see <see cref="Dismiss"/>). Extracted out of <see cref="MainWindow"/> so the idle-timer state
/// isn't mixed in with navigation (see <see cref="FolderNavigator"/>) and launch-workflow concerns.
/// </summary>
public sealed class ScreensaverController : IDisposable
{
    private readonly TvLauncherOptions _options;
    private readonly ILauncherSettingsStore _settingsStore;
    private readonly Panel _overlay;
    private readonly IReadOnlyList<IScreensaverScene> _scenes;
    private readonly Func<bool> _isSuppressed;
    private readonly DispatcherTimer _idleCheckTimer;

    private DateTime _lastActivityUtc = DateTime.UtcNow;
    private IScreensaverScene? _activeScene;

    public bool IsActive => _activeScene is not null;

    /// <param name="overlay">The full-window panel the scenes are drawn in.</param>
    /// <param name="isSuppressed">
    /// Checked before showing the screensaver, e.g. so it doesn't take over while an emulator
    /// has focus (already covering the window and constantly redrawing, so there's no burn-in
    /// risk from TvLauncher itself) or while a modal overlay/error message is up.
    /// </param>
    public ScreensaverController(
        TvLauncherOptions options,
        ILauncherSettingsStore settingsStore,
        Panel overlay,
        IEnumerable<IScreensaverScene> scenes,
        Func<bool> isSuppressed)
    {
        _options = options;
        _settingsStore = settingsStore;
        _overlay = overlay;
        _scenes = [.. scenes];
        _isSuppressed = isSuppressed;

        foreach (var scene in _scenes)
        {
            scene.View.Visibility = Visibility.Collapsed;
            _overlay.Children.Add(scene.View);
        }

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

    /// <summary>
    /// Shows the chosen screensaver now, without waiting to go idle, e.g. to preview a newly
    /// chosen one. Any input dismisses it as usual.
    /// </summary>
    public void Show()
    {
        if (IsActive)
            return;

        var style = _settingsStore.Current.Screensaver;
        var scene = _scenes.FirstOrDefault(s => s.ScreensaverStyle == style) ?? _scenes.FirstOrDefault();
        if (scene is null)
            return;

        _activeScene = scene;
        scene.View.Visibility = Visibility.Visible;
        _overlay.Visibility = Visibility.Visible;
        _overlay.Opacity = 0;
        Motion.FadeTo(_overlay, 1, Motion.Crossfade);
        scene.Start();
    }

    /// <summary>Hides the screensaver at once, so the launcher responds to the same press.</summary>
    public void Dismiss()
    {
        if (_activeScene is not { } scene)
            return;

        scene.Stop();
        scene.View.Visibility = Visibility.Collapsed;
        _overlay.BeginAnimation(UIElement.OpacityProperty, null);
        _overlay.Visibility = Visibility.Collapsed;
        _activeScene = null;
    }

    private void CheckIdleTimeout()
    {
        if (IsActive || _isSuppressed())
            return;

        var idleFor = DateTime.UtcNow - _lastActivityUtc;
        if (idleFor.TotalMinutes >= _options.ScreensaverIdleMinutes)
            Show();
    }

    public void Dispose()
    {
        _idleCheckTimer.Stop();
        Dismiss();
    }
}
