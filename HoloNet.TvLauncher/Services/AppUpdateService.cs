using System.Reflection;
using HoloNet.TvLauncher.Configuration;
using Microsoft.Extensions.Logging;
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

public enum AppUpdateCheckOutcome
{
    /// <summary>A newer version is downloaded and ready to install (see <see cref="AppUpdateCheckResult.Update"/>).</summary>
    UpdateReady,

    /// <summary>The running version is the latest release.</summary>
    UpToDate,

    /// <summary>
    /// This copy can't update itself: it wasn't installed with the release's Setup.exe (e.g. a
    /// portable or dev build, see <see cref="Velopack.UpdateManager.IsInstalled"/>).
    /// </summary>
    NotInstalled,

    /// <summary>The check or download failed, e.g. offline or rate-limited by GitHub.</summary>
    Failed
}

/// <summary>
/// What an update check found. Built only through the factory members, so <see cref="Update"/>
/// is set exactly when <see cref="Outcome"/> is <see cref="AppUpdateCheckOutcome.UpdateReady"/>.
/// </summary>
public sealed record AppUpdateCheckResult
{
    private AppUpdateCheckResult(AppUpdateCheckOutcome outcome, AppUpdateInfo? update)
    {
        Outcome = outcome;
        Update = update;
    }

    public static AppUpdateCheckResult UpToDate { get; } = new(AppUpdateCheckOutcome.UpToDate, null);

    public static AppUpdateCheckResult NotInstalled { get; } = new(AppUpdateCheckOutcome.NotInstalled, null);

    public static AppUpdateCheckResult Failed { get; } = new(AppUpdateCheckOutcome.Failed, null);

    public AppUpdateCheckOutcome Outcome { get; }

    public AppUpdateInfo? Update { get; }

    public static AppUpdateCheckResult Ready(AppUpdateInfo update) => new(AppUpdateCheckOutcome.UpdateReady, update);
}

public interface IAppUpdateService
{
    /// <summary>
    /// The version of the app currently running, for display purposes (e.g. in the TV menu
    /// header). Uses Velopack's installed version when running as an installed copy, falling
    /// back to the assembly's informational version for portable/dev builds where Velopack's
    /// <see cref="Velopack.UpdateManager"/> is unavailable.
    /// </summary>
    string CurrentVersion { get; }

    /// <summary>
    /// Checks GitHub Releases for a newer version and, if one exists, downloads it. Never throws:
    /// a failed check is reported as <see cref="AppUpdateCheckOutcome.Failed"/>. Whether to check
    /// at all (see <see cref="TvLauncherOptions.AutoUpdateEnabled"/>) is up to the caller (see
    /// <see cref="AppUpdateTracker"/>).
    /// </summary>
    Task<AppUpdateCheckResult> CheckAndDownloadUpdateAsync(CancellationToken cancellationToken = default);

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
    private readonly ILogger<AppUpdateService> _logger;
    private readonly Lazy<UpdateManager?> _updateManager;

    public AppUpdateService(IOptions<TvLauncherOptions> options, ILogger<AppUpdateService> logger)
    {
        _options = options.Value;
        _logger = logger;
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

    public string CurrentVersion
    {
        get
        {
            var updateManager = _updateManager.Value;
            if (updateManager is { IsInstalled: true } && updateManager.CurrentVersion is not null)
                return updateManager.CurrentVersion.ToString();

            // Portable/dev build — fall back to the assembly's informational version (set from
            // the csproj's <Version>, matching the tag used for the last release build).
            var assembly = Assembly.GetExecutingAssembly();
            var infoVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            return string.IsNullOrWhiteSpace(infoVersion) ? "dev" : infoVersion;
        }
    }

    public async Task<AppUpdateCheckResult> CheckAndDownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        var updateManager = _updateManager.Value;
        if (updateManager is null || !updateManager.IsInstalled)
            return AppUpdateCheckResult.NotInstalled;

        try
        {
            var updateInfo = await updateManager.CheckForUpdatesAsync();
            if (updateInfo is null)
                return AppUpdateCheckResult.UpToDate;

            // Velopack skips the download when this version's package is already on disk, so
            // finding the same pending update again on a later check costs only the lookup.
            await updateManager.DownloadUpdatesAsync(updateInfo, cancelToken: cancellationToken);

            return AppUpdateCheckResult.Ready(new AppUpdateInfo(
                updateManager.CurrentVersion?.ToString() ?? "unknown",
                updateInfo.TargetFullRelease.Version.ToString(),
                updateInfo.TargetFullRelease.NotesMarkdown,
                updateInfo));
        }
        catch (Exception exception)
        {
            // Network hiccups, GitHub rate limits, etc. An automatic check stays quiet on screen
            // and the next one tries again; a check the player asked for says it failed. Logged
            // either way, so updates that keep failing can be diagnosed.
            _logger.LogWarning(exception, "Checking {RepositoryUrl} for a TvLauncher update failed", _options.UpdateRepositoryUrl);
            return AppUpdateCheckResult.Failed;
        }
    }

    public void ApplyUpdateAndRestart(AppUpdateInfo updateInfo)
    {
        var updateManager = _updateManager.Value
            ?? throw new InvalidOperationException("Velopack update manager is unavailable in this process.");

        updateManager.ApplyUpdatesAndRestart(updateInfo.VelopackUpdateInfo.TargetFullRelease);
    }
}
