using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HoloNet.Shared.HealthChecks;

/// <summary>
/// Verifies that a configured media directory exists and is readable.
/// Registered per-service with the configured path (video/photo/game directory).
/// </summary>
public class MediaDirectoryHealthCheck(string directoryPath) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return Task.FromResult(HealthCheckResult.Unhealthy("Media directory path is not configured."));

        if (!Directory.Exists(directoryPath))
            return Task.FromResult(HealthCheckResult.Unhealthy($"Media directory not found: '{directoryPath}'."));

        try
        {
            // Confirm the directory is actually readable, not just present.
            using var enumerator = Directory.EnumerateFileSystemEntries(directoryPath).GetEnumerator();
            enumerator.MoveNext();
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Media directory is not readable: '{directoryPath}'.", ex));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Media directory OK: '{directoryPath}'."));
    }
}
