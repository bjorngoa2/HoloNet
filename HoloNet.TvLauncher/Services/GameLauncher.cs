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
}

/// <summary>
/// Resolves a platform to its configured emulator and shells out to it. Never talks to the
/// bus/relay/etc. — purely a local process launcher, mirroring the "facade over complexity"
/// pattern used by HoloNet's server-side services.
/// </summary>
public class GameLauncher(IOptions<TvLauncherOptions> options) : IGameLauncher
{
    private readonly TvLauncherOptions _options = options.Value;

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

            await process.WaitForExitAsync(cancellationToken);
            return new GameLaunchResult(LaunchOutcome.Success);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new GameLaunchResult(LaunchOutcome.LaunchFailed, ex.Message);
        }
    }
}
