using HoloNet.TvLauncher.Configuration;

namespace HoloNet.TvLauncher.Services;

/// <summary>
/// Decides when to look for an update and remembers what was found, so the launcher (which can
/// stay open on the TV for days) notices a new release without being restarted. Checks run in
/// the background at startup, every <see cref="TvLauncherOptions.UpdateCheckIntervalHours"/> and
/// when the screensaver comes on, and on demand from Settings (see <see cref="CheckNowAsync"/>).
/// Overlapping requests share one check, so GitHub is never asked twice at once.
/// </summary>
public sealed class AppUpdateTracker(IAppUpdateService updateService, TvLauncherOptions options, TimeProvider timeProvider)
{
    /// <summary>
    /// How old the last check must be before the screensaver coming on starts another, so a TV
    /// that keeps dozing off doesn't ask GitHub every few minutes.
    /// </summary>
    public static readonly TimeSpan ScreensaverCheckMinimumAge = TimeSpan.FromHours(1);

    private Task<AppUpdateCheckResult>? _check;
    private DateTimeOffset? _lastCheckedAt;
    private string? _dismissedVersion;

    /// <summary>The newest downloaded update that's ready to install, if any.</summary>
    public AppUpdateInfo? PendingUpdate { get; private set; }

    /// <summary>
    /// Whether to offer <see cref="PendingUpdate"/>: one is downloaded and the player hasn't put
    /// that version off with "Later" (see <see cref="Dismiss"/>).
    /// </summary>
    public bool IsPending => PendingUpdate is { } update && update.NewVersion != _dismissedVersion;

    /// <summary>Raised when <see cref="IsPending"/> or <see cref="PendingUpdate"/> may have changed.</summary>
    public event Action? Changed;

    /// <summary>
    /// Checks for an update and downloads it, unless automatic updates are off
    /// (<see cref="TvLauncherOptions.AutoUpdateEnabled"/>) or the last check finished less than
    /// <paramref name="minimumAge"/> ago. Failures are silent; the next check tries again.
    /// </summary>
    public async Task CheckInBackgroundAsync(TimeSpan? minimumAge = null)
    {
        if (!options.AutoUpdateEnabled)
            return;

        if (minimumAge is { } age && _lastCheckedAt is { } last && timeProvider.GetUtcNow() - last < age)
            return;

        await CheckAsync();
    }

    /// <summary>
    /// Checks for an update now, because the player asked, even if automatic updates are off.
    /// A version put off with "Later" is offered again, since asking means they want it.
    /// </summary>
    public async Task<AppUpdateCheckResult> CheckNowAsync()
    {
        var result = await CheckAsync();
        if (result.Update is not null && _dismissedVersion is not null)
        {
            _dismissedVersion = null;
            Changed?.Invoke();
        }

        return result;
    }

    /// <summary>
    /// Stops offering the pending version ("Later"). A newer release, or the player checking
    /// from Settings, offers an update again.
    /// </summary>
    public void Dismiss()
    {
        if (PendingUpdate is not { } update)
            return;

        _dismissedVersion = update.NewVersion;
        Changed?.Invoke();
    }

    private Task<AppUpdateCheckResult> CheckAsync()
    {
        if (_check is { IsCompleted: false } running)
            return running;

        return _check = RunCheckAsync();
    }

    private async Task<AppUpdateCheckResult> RunCheckAsync()
    {
        try
        {
            var result = await updateService.CheckAndDownloadUpdateAsync();
            if (result.Update is { } update)
            {
                PendingUpdate = update;
                Changed?.Invoke();
            }

            return result;
        }
        finally
        {
            _lastCheckedAt = timeProvider.GetUtcNow();
        }
    }
}
