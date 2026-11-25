using System.IO;
using HoloNet.TvLauncher.Configuration;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public enum KioskBrowserLaunchOutcome
{
    Success,
    NotConfigured,
    LaunchFailed
}

public record KioskBrowserLaunchResult(KioskBrowserLaunchOutcome Outcome, string? ErrorMessage = null);

public interface IKioskBrowserLauncher
{
    /// <summary>
    /// Opens <paramref name="url"/> in Firefox kiosk mode (fullscreen, no tabs/address bar) using
    /// a dedicated HoloNet profile (see <see cref="TvLauncherOptions.KioskFirefoxProfilePath"/>),
    /// and awaits the browser exiting - the same "managed session" shape as
    /// <see cref="IGameLauncher.LaunchAsync"/>, so the picker stays hidden until the player
    /// returns via the quit combo (see <see cref="QuitAsync"/>).
    /// </summary>
    Task<KioskBrowserLaunchResult> RunAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the currently-running kiosk browser session (if any), so the in-progress
    /// <see cref="RunAsync"/> call returns. Returns <c>false</c> if no session is running.
    /// </summary>
    Task<bool> QuitAsync();
}

/// <summary>
/// Runs Firefox as a kiosk-mode "app" for web shortcut tiles (see
/// <see cref="ShortcutMapping.Kiosk"/>). Firefox is used (rather than the default browser or an
/// embedded WebView2) because it's the only mainstream browser still supporting the full
/// uBlock Origin - see <c>HoloNet.TvLauncher/Firefox/README.md</c> for force-installing it.
///
/// A dedicated profile plus <c>--no-remote</c> guarantees a separate Firefox instance: without
/// it, an already-running everyday Firefox (e.g. one left open by HoloNet.PrimePlayer) would
/// swallow the URL as a new, non-kiosk tab and the launched process would exit immediately.
/// It also makes quitting safe - killing this process tree never touches your other Firefox
/// windows. <c>--wait-for-browser</c> keeps Firefox's launcher process alive (as the parent of
/// the real browser process) until the browser exits, so there's one process to await/kill.
/// </summary>
public class KioskBrowserLauncher(IOptions<TvLauncherOptions> options) : IKioskBrowserLauncher
{
    private static readonly string[] DefaultFirefoxPaths =
    [
        @"C:\Program Files\Mozilla Firefox\firefox.exe",
        @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"
    ];

    // Written to the dedicated profile's user.js on every launch, so it's always a TV-friendly
    // kiosk: quitting kills the process, so suppress the "restore previous session?" page that
    // would otherwise greet the next launch, plus first-run/default-browser prompts that need a
    // mouse to dismiss.
    private const string UserPreferences = """
        user_pref("browser.sessionstore.resume_from_crash", false);
        user_pref("browser.shell.checkDefaultBrowser", false);
        user_pref("browser.aboutwelcome.enabled", false);
        user_pref("startup.homepage_welcome_url", "");
        user_pref("browser.startup.homepage_override.mstone", "ignore");
        user_pref("datareporting.policy.dataSubmissionPolicyBypassNotification", true);
        user_pref("media.autoplay.default", 0);
        """;

    private readonly TvLauncherOptions _options = options.Value;
    private System.Diagnostics.Process? _currentProcess;

    public async Task<KioskBrowserLaunchResult> RunAsync(string url, CancellationToken cancellationToken = default)
    {
        var firefoxPath = ResolveFirefoxPath();
        if (firefoxPath is null)
        {
            return new KioskBrowserLaunchResult(
                KioskBrowserLaunchOutcome.NotConfigured,
                string.IsNullOrWhiteSpace(_options.FirefoxPath)
                    ? "Firefox isn't installed (or set TvLauncher:FirefoxPath)."
                    : $"Firefox isn't installed at the configured path:\n{_options.FirefoxPath}");
        }

        try
        {
            var profilePath = ResolveProfilePath();
            Directory.CreateDirectory(profilePath);
            await File.WriteAllTextAsync(Path.Combine(profilePath, "user.js"), UserPreferences, cancellationToken);

            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = firefoxPath,
                ArgumentList = { "--wait-for-browser", "--no-remote", "--profile", profilePath, "--kiosk", url },
                UseShellExecute = false
            });

            if (process is null)
                return new KioskBrowserLaunchResult(KioskBrowserLaunchOutcome.LaunchFailed, "Firefox could not be started.");

            _currentProcess = process;
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                _currentProcess = null;
            }

            return new KioskBrowserLaunchResult(KioskBrowserLaunchOutcome.Success);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return new KioskBrowserLaunchResult(KioskBrowserLaunchOutcome.LaunchFailed, ex.Message);
        }
    }

    public async Task<bool> QuitAsync()
    {
        var process = _currentProcess;
        if (process is null || process.HasExited)
            return false;

        try
        {
            // The launcher process has no window of its own (so CloseMainWindow can't work), and
            // a graceful close of the kiosk window could be blocked by a page's "leave site?"
            // prompt the player can't answer - kill the tree, as for hidden emulators.
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        catch (InvalidOperationException)
        {
            // Already exited between the HasExited check and Kill.
        }

        return true;
    }

    private string? ResolveFirefoxPath()
    {
        if (!string.IsNullOrWhiteSpace(_options.FirefoxPath))
            return File.Exists(_options.FirefoxPath) ? _options.FirefoxPath : null;

        return DefaultFirefoxPaths.FirstOrDefault(File.Exists);
    }

    private string ResolveProfilePath() =>
        string.IsNullOrWhiteSpace(_options.KioskFirefoxProfilePath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoloNet", "TvLauncher", "FirefoxKioskProfile")
            : Environment.ExpandEnvironmentVariables(_options.KioskFirefoxProfilePath);
}
