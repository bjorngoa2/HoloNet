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

    public async Task<GameLaunchResult> LaunchAsync(LaunchIntentDto launchIntent, CancellationToken cancellationToken = default)
    {
        if (!_options.EmulatorMappings.TryGetValue(launchIntent.Platform, out var mapping)
            || string.IsNullOrWhiteSpace(mapping.ExecutablePath))
        {
            return new GameLaunchResult(
                LaunchOutcome.NoEmulatorConfigured,
                $"No emulator is configured for platform \"{launchIntent.Platform}\".");
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
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                _currentProcess = null;
                _currentMapping = null;
            }

            return new GameLaunchResult(LaunchOutcome.Success);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new GameLaunchResult(LaunchOutcome.LaunchFailed, ex.Message);
        }
    }

    public async Task<bool> QuitCurrentGameAsync()
    {
        var process = _currentProcess;
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
}
