using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public enum LaunchOutcome
{
    Success,
    NoEmulatorConfigured,
    LaunchFailed
}

public record GameLaunchResult(LaunchOutcome Outcome, string? ErrorMessage = null);

public interface IGameLauncher
{
    /// <summary>
    /// Starts the emulator mapped to <paramref name="launchIntent"/>'s platform, over the
    /// game's network path, and awaits the emulator process exiting (so the caller can
    /// re-show the picker UI once the player quits).
    /// </summary>
    Task<GameLaunchResult> LaunchAsync(LaunchIntentDto launchIntent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests that the currently-running emulator (if any) close, so the in-progress
    /// <see cref="LaunchAsync"/> call returns and the picker can be shown again. Behavior is
    /// controlled by the launched game's <see cref="EmulatorMapping.ForceKillOnQuit"/>: when
    /// <c>true</c>, the process is killed immediately (no confirmation dialog can appear —
    /// intended for emulators running hidden/headless); when <c>false</c> (default), a
    /// graceful close is requested first and only force-killed after a grace period if it
    /// doesn't respond (lets a visible emulator show its own "are you sure?"/save prompts).
    /// Returns <c>false</c> if no emulator is currently running.
    /// </summary>
    Task<bool> QuitCurrentGameAsync();

    /// <summary>
    /// Title of the game currently running (matches the emulator process being tracked by
    /// <see cref="QuitCurrentGameAsync"/>), or <c>null</c> if nothing is running. Exposed so the
    /// quit flow can capture a "where I currently am" showcase screenshot tagged to the right
    /// game before the emulator actually closes.
    /// </summary>
    string? CurrentGameTitle { get; }

    /// <summary>
    /// Win32 window handle (HWND) of the currently-running emulator's main window, or
    /// <see cref="IntPtr.Zero"/> if nothing is running or the window hasn't been created yet.
    /// Used to capture a screenshot of the emulator specifically (see
    /// <see cref="IGameScreenshotService"/>) rather than whatever happens to be on top of the
    /// whole screen at the moment of capture.
    /// </summary>
    IntPtr CurrentEmulatorWindowHandle { get; }

    /// <summary>
    /// Opens <paramref name="url"/> in the system default browser and returns immediately —
    /// unlike <see cref="LaunchAsync"/>, nothing is tracked or awaited, since a browser tab
    /// isn't a managed emulator session with its own quit combo/lifetime. Used for the picker's
    /// non-game shortcut tiles (see <see cref="Configuration.TvLauncherOptions.Shortcuts"/>).
    /// </summary>
    bool LaunchShortcut(string url);

    /// <summary>
    /// Platform names (from <see cref="TvLauncherOptions.EmulatorMappings"/>) whose configured
    /// <see cref="EmulatorMapping.ExecutablePath"/> doesn't exist on this PC — checked once at
    /// startup so the picker can surface a "these platforms won't work" warning up front,
    /// rather than the player only discovering it partway through trying to launch a game (see
    /// <see cref="Views.MainWindow"/>'s status line).
    /// </summary>
    IReadOnlyList<string> GetMissingEmulatorPlatforms();
}

/// <summary>
/// Resolves a platform to its configured emulator and shells out to it. Never talks to the
/// bus/relay/etc. — purely a local process launcher, mirroring the "facade over complexity"
/// pattern used by HoloNet's server-side services.
/// </summary>
public class GameLauncher(IOptions<TvLauncherOptions> options) : IGameLauncher
{
    private static readonly TimeSpan GracefulQuitTimeout = TimeSpan.FromSeconds(5);

    private readonly TvLauncherOptions _options = options.Value;
    private System.Diagnostics.Process? _currentProcess;
    private EmulatorMapping? _currentMapping;
    private string? _currentGameTitle;

    public string? CurrentGameTitle => _currentGameTitle;

    public IntPtr CurrentEmulatorWindowHandle
    {
        get
        {
            if (_currentProcess is not { HasExited: false } process)
                return IntPtr.Zero;

            // The Process object caches MainWindowHandle from when it was first queried; the
            // emulator's actual window may not have existed yet at that point (or PCSX2, when
            // launched hidden then later un-hidden, can create/replace it later) — refresh to
            // get the current handle.
            process.Refresh();
            return process.MainWindowHandle;
        }
    }

    public async Task<GameLaunchResult> LaunchAsync(LaunchIntentDto launchIntent, CancellationToken cancellationToken = default)
    {
        if (!_options.EmulatorMappings.TryGetValue(launchIntent.Platform, out var mapping)
            || string.IsNullOrWhiteSpace(mapping.ExecutablePath))
        {
            return new GameLaunchResult(
                LaunchOutcome.NoEmulatorConfigured,
                $"No emulator is configured for platform \"{launchIntent.Platform}\".");
        }

        if (!System.IO.File.Exists(mapping.ExecutablePath))
        {
            return new GameLaunchResult(
                LaunchOutcome.NoEmulatorConfigured,
                $"The emulator configured for \"{launchIntent.Platform}\" isn't installed on this PC.\n" +
                $"Expected it at: {mapping.ExecutablePath}");
        }

        var arguments = mapping.ArgumentsTemplate.Replace("{NetworkPath}", launchIntent.NetworkPath);
        if (mapping.HideWindow && !string.IsNullOrWhiteSpace(mapping.HideWindowArgument))
            arguments = $"{mapping.HideWindowArgument} {arguments}";

        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = mapping.ExecutablePath,
                Arguments = arguments,
                UseShellExecute = false
            });

            if (process is null)
                return new GameLaunchResult(LaunchOutcome.LaunchFailed, "The emulator process could not be started.");

            _currentProcess = process;
            _currentMapping = mapping;
            _currentGameTitle = launchIntent.Title;
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                _currentProcess = null;
                _currentMapping = null;
                _currentGameTitle = null;
            }

            return new GameLaunchResult(LaunchOutcome.Success);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new GameLaunchResult(LaunchOutcome.LaunchFailed, ex.Message);
        }
    }

    public async Task<bool> QuitCurrentGameAsync()
    {        var process = _currentProcess;
        if (process is null || process.HasExited)
            return false;

        try
        {
            if (_currentMapping?.ForceKillOnQuit == true)
            {
                // Emulators running hidden (-nogui or similar) can still pop up a visible
                // "are you sure?" confirmation dialog in response to a graceful WM_CLOSE (via
                // CloseMainWindow) — defeating the point of hiding them. There's no interactive
                // save flow the player can respond to on a hidden window anyway, so kill it
                // directly.
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                return true;
            }

            // Ask nicely first (posts WM_CLOSE to the emulator's main window) so a visible
            // emulator can show its own save/confirmation prompts, same as clicking its close
            // button.
            process.CloseMainWindow();

            using var timeoutCts = new CancellationTokenSource(GracefulQuitTimeout);
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Didn't close in time (e.g. showing an "unsaved progress" prompt, or hung) —
                // force it so the picker isn't stuck waiting indefinitely.
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }

            return true;
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the HasExited check and CloseMainWindow/Kill.
            return true;
        }
    }

    public bool LaunchShortcut(string url)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return process is not null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    public IReadOnlyList<string> GetMissingEmulatorPlatforms()
    {
        return _options.EmulatorMappings
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value.ExecutablePath) && !System.IO.File.Exists(kvp.Value.ExecutablePath))
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
