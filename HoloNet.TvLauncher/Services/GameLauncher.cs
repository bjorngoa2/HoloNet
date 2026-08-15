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
    /// <see cref="LaunchAsync"/> call returns and the picker can be shown again. Tries a
    /// graceful close first, then force-kills the process tree if it doesn't respond in time.
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
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            finally
            {
                _currentProcess = null;
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
            // Ask nicely first (posts WM_CLOSE to the emulator's main window) so it can save
            // state/settings normally, same as the player clicking its close button.
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
