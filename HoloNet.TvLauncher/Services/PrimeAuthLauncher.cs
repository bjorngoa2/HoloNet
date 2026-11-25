using HoloNet.TvLauncher.Configuration;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public enum PrimeAuthLaunchOutcome
{
    Success,
    NotConfigured,
    LaunchFailed
}

public record PrimeAuthLaunchResult(PrimeAuthLaunchOutcome Outcome, string? ErrorMessage = null);

public interface IPrimeAuthLauncher
{
    /// <summary>
    /// Starts the configured <c>HoloNet.PrimeAuth</c> tool (see
    /// <see cref="TvLauncherOptions.PrimeAuthToolPath"/>) as a child process, passing it
    /// HoloNet.Video's base URL (derived from <see cref="TvLauncherOptions.VideoCatalogApiBaseUrl"/>)
    /// so it knows where to push the captured session cookie, and awaits it exiting - mirroring
    /// <see cref="IGameLauncher.LaunchAsync"/>'s "start a process, wait for it to close" shape.
    /// </summary>
    Task<PrimeAuthLaunchResult> RunLoginFlowAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Launches the standalone <c>HoloNet.PrimeAuth</c> browser-login tool and waits for it to exit,
/// same pattern as <see cref="GameLauncher"/> launching an emulator - this app just starts the
/// process and waits, it never talks Playwright/browser automation itself.
/// </summary>
public class PrimeAuthLauncher(IOptions<TvLauncherOptions> options) : IPrimeAuthLauncher
{
    private readonly TvLauncherOptions _options = options.Value;

    public async Task<PrimeAuthLaunchResult> RunLoginFlowAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.PrimeAuthToolPath))
        {
            return new PrimeAuthLaunchResult(
                PrimeAuthLaunchOutcome.NotConfigured,
                "TvLauncher:PrimeAuthToolPath isn't configured.");
        }

        if (!System.IO.File.Exists(_options.PrimeAuthToolPath))
        {
            return new PrimeAuthLaunchResult(
                PrimeAuthLaunchOutcome.NotConfigured,
                $"HoloNet.PrimeAuth isn't installed at the configured path:\n{_options.PrimeAuthToolPath}");
        }

        // HoloNet.PrimeAuth needs HoloNet.Video's base URL (not the /api/v1/catalog suffix) to
        // know where to push the captured cookie - derive it from VideoCatalogApiBaseUrl by
        // stripping the catalog API path back off.
        var videoBaseUrl = _options.VideoCatalogApiBaseUrl
            .Replace("/api/v1/catalog", string.Empty, StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

        if (string.IsNullOrWhiteSpace(videoBaseUrl))
        {
            return new PrimeAuthLaunchResult(
                PrimeAuthLaunchOutcome.NotConfigured,
                "TvLauncher:VideoCatalogApiBaseUrl isn't configured.");
        }

        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _options.PrimeAuthToolPath,
                Arguments = videoBaseUrl,
                UseShellExecute = false
            });

            if (process is null)
                return new PrimeAuthLaunchResult(PrimeAuthLaunchOutcome.LaunchFailed, "The login tool could not be started.");

            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0
                ? new PrimeAuthLaunchResult(PrimeAuthLaunchOutcome.Success)
                : new PrimeAuthLaunchResult(PrimeAuthLaunchOutcome.LaunchFailed, "Login was not completed successfully.");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new PrimeAuthLaunchResult(PrimeAuthLaunchOutcome.LaunchFailed, ex.Message);
        }
    }
}
