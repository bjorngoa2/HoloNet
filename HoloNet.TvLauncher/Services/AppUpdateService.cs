using HoloNet.TvLauncher.Configuration;
using Microsoft.Extensions.Options;
using Velopack;
using Velopack.Sources;

namespace HoloNet.TvLauncher.Services;

/// <summary>
/// Info about a downloaded, ready-to-apply update — deliberately opaque about the underlying
/// Velopack type so callers (e.g. <see cref="Views.MainWindow"/>) only need the version/notes to
/// show a banner and an opaque token to hand back to <see cref="IAppUpdateService.ApplyUpdateAndRestart"/>.
/// </summary>
public sealed record AppUpdateInfo(string CurrentVersion, string NewVersion, string? ReleaseNotesMarkdown, UpdateInfo VelopackUpdateInfo);

public interface IAppUpdateService
{
    /// <summary>
    /// Checks GitHub Releases for a newer version and, if one exists, downloads it in the
    /// background. Returns <c>null</c> if already up to date, if this isn't a Velopack-installed
    /// copy (e.g. a portable/dev build — see <see cref="Velopack.UpdateManager.IsInstalled"/>),
    /// or if the check/download failed (e.g. offline) — callers should treat all of these the
    /// same way: silently do nothing.
    /// </summary>
    Task<AppUpdateInfo?> CheckAndDownloadUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an already-downloaded update and restarts the app as the new version. This exits
    /// the current process — Velopack's <c>Update.exe</c> performs the actual file swap once
    /// this process has ended (a running .exe can't overwrite itself), then relaunches.
    /// </summary>
    void ApplyUpdateAndRestart(AppUpdateInfo updateInfo);
}

public sealed class AppUpdateService : IAppUpdateService
{
    private readonly TvLauncherOptions _options;
    private readonly Lazy<UpdateManager?> _updateManager;

    public AppUpdateService(IOptions<TvLauncherOptions> options)
    {
        _options = options.Value;
        _updateManager = new Lazy<UpdateManager?>(() =>
        {
            try
            {
                var source = new GithubSource(_options.UpdateRepositoryUrl, accessToken: null, prerelease: false);
                return new UpdateManager(source);
            }
            catch (InvalidOperationException)
            {
                // No Velopack locator is available outside of an installed app process (e.g. a
                // portable/dev build run without going through Setup.exe first) — VelopackApp
                // .Build().Run() never ran, so UpdateManager's constructor throws. Treat this the
                // same as "updates unavailable" rather than surfacing it as an error.
                return null;
            }
        });
    }

    public async Task<AppUpdateInfo?> CheckAndDownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.AutoUpdateEnabled)
            return null;

        var updateManager = _updateManager.Value;
        if (updateManager is null || !updateManager.IsInstalled)
            return null;

        try
        {
            var updateInfo = await updateManager.CheckForUpdatesAsync();
            if (updateInfo is null)
                return null;

            await updateManager.DownloadUpdatesAsync(updateInfo, cancelToken: cancellationToken);

            return new AppUpdateInfo(
                updateManager.CurrentVersion?.ToString() ?? "unknown",
                updateInfo.TargetFullRelease.Version.ToString(),
                updateInfo.TargetFullRelease.NotesMarkdown,
                updateInfo);
        }
        catch (Exception)
        {
            // Network hiccups, GitHub rate limits, etc. — an update check is opportunistic, not
            // essential, so failures here are silently retried on the next launch rather than
            // surfaced to the player.
            return null;
        }
    }

    public void ApplyUpdateAndRestart(AppUpdateInfo updateInfo)
    {
        var updateManager = _updateManager.Value
            ?? throw new InvalidOperationException("Velopack update manager is unavailable in this process.");

        updateManager.ApplyUpdatesAndRestart(updateInfo.VelopackUpdateInfo.TargetFullRelease);
    }
}
