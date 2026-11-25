using HoloNet.TvLauncher.Configuration;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public enum PrimePlayerLaunchOutcome
{
    Success,
    NotConfigured,
    LaunchFailed
}

public record PrimePlayerLaunchResult(PrimePlayerLaunchOutcome Outcome, string? ErrorMessage = null);

public interface IPrimePlayerLauncher
{
    /// <summary>
    /// Starts the configured <c>HoloNet.PrimePlayer</c> tool (see
    /// <see cref="TvLauncherOptions.PrimePlayerToolPath"/>) as a child process, passing it the
    /// title's deep link URL and display title, and awaits it exiting - mirroring
    /// <see cref="IPrimeAuthLauncher.RunLoginFlowAsync"/>'s "start a process, wait for it to
    /// close" shape. The tool itself launches a real Firefox window, clicks Play, and goes
    /// fullscreen, then exits on its own - this method's task completes once that whole flow is
    /// done (or has failed), at which point the picker can hide its "Launching..." overlay.
    ///
    /// When <paramref name="restart"/> is <c>true</c>, PrimePlayer starts playback as normal and
    /// then clicks the player's Seek bar back to the start - use this for a "Start over" action so
    /// a previously-watched episode/movie doesn't just resume at (or near) the very end.
    /// </summary>
    Task<PrimePlayerLaunchResult> PlayAsync(string deepLinkUrl, string title, bool restart = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies the browser PrimePlayer plays titles in among the system media sessions (see
    /// <see cref="IMediaSessionService"/>), so the launcher can control playback once it starts.
    /// </summary>
    string MediaSourceAppId { get; }
}

/// <summary>
/// Launches the standalone <c>HoloNet.PrimePlayer</c> tool and waits for it to exit, same
/// pattern as <see cref="PrimeAuthLauncher"/> launching the browser-login tool - this app just
/// starts the process and waits, it never talks UI Automation/browser automation itself.
/// </summary>
public class PrimePlayerLauncher(IOptions<TvLauncherOptions> options) : IPrimePlayerLauncher
{
    private readonly TvLauncherOptions _options = options.Value;

    // PrimePlayer always drives Firefox, which publishes its media session as "firefox.exe".
    public string MediaSourceAppId => "firefox";

    public async Task<PrimePlayerLaunchResult> PlayAsync(string deepLinkUrl, string title, bool restart = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.PrimePlayerToolPath))
        {
            return new PrimePlayerLaunchResult(
                PrimePlayerLaunchOutcome.NotConfigured,
                "TvLauncher:PrimePlayerToolPath isn't configured.");
        }

        var toolPath = ResolveToolPath(_options.PrimePlayerToolPath, AppContext.BaseDirectory);
        if (!System.IO.File.Exists(toolPath))
        {
            return new PrimePlayerLaunchResult(
                PrimePlayerLaunchOutcome.NotConfigured,
                $"HoloNet.PrimePlayer isn't installed at the configured path:\n{toolPath}");
        }

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = toolPath,
                ArgumentList = { deepLinkUrl, title },
                UseShellExecute = false
            };
            if (restart)
                startInfo.ArgumentList.Add("--restart");

            using var process = System.Diagnostics.Process.Start(startInfo);

            if (process is null)
                return new PrimePlayerLaunchResult(PrimePlayerLaunchOutcome.LaunchFailed, "HoloNet.PrimePlayer could not be started.");

            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0
                ? new PrimePlayerLaunchResult(PrimePlayerLaunchOutcome.Success)
                : new PrimePlayerLaunchResult(PrimePlayerLaunchOutcome.LaunchFailed, "Couldn't automatically start playback - the deep link will open in your browser instead.");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new PrimePlayerLaunchResult(PrimePlayerLaunchOutcome.LaunchFailed, ex.Message);
        }
    }

    /// <summary>
    /// A relative <paramref name="configuredPath"/> is relative to the launcher's own folder
    /// (<paramref name="baseDirectory"/>), where the release bundles the tool - not to the working
    /// directory, which depends on how the launcher was started.
    /// </summary>
    internal static string ResolveToolPath(string configuredPath, string baseDirectory) =>
        System.IO.Path.GetFullPath(configuredPath, baseDirectory);
}
