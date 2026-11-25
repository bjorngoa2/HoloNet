using System.Windows.Threading;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Starts <see cref="AppUpdateTracker"/>'s background checks while the launcher stays open: once
/// at startup, every <see cref="TvLauncherOptions.UpdateCheckIntervalHours"/>, and when the
/// screensaver comes on, since the TV is unattended then and an update downloaded meanwhile is
/// ready to offer when the player comes back. Everything runs on the UI thread.
/// </summary>
public sealed class AppUpdateScheduler : IDisposable
{
    private readonly AppUpdateTracker _tracker;
    private readonly ScreensaverController _screensaver;
    private readonly DispatcherTimer _timer;

    public AppUpdateScheduler(AppUpdateTracker tracker, TvLauncherOptions options, ScreensaverController screensaver)
    {
        _tracker = tracker;
        _screensaver = screensaver;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(options.UpdateCheckIntervalHours) };
        _timer.Tick += OnTimerTick;
        _screensaver.Shown += OnScreensaverShown;
    }

    public void Start()
    {
        _ = _tracker.CheckInBackgroundAsync();
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e) => _ = _tracker.CheckInBackgroundAsync();

    private void OnScreensaverShown() =>
        _ = _tracker.CheckInBackgroundAsync(AppUpdateTracker.ScreensaverCheckMinimumAge);

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _screensaver.Shown -= OnScreensaverShown;
    }
}
