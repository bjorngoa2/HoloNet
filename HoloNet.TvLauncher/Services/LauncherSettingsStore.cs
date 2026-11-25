using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public interface ILauncherSettingsStore
{
    /// <summary>The settings in effect: the last ones loaded or saved, or the defaults.</summary>
    LauncherSettings Current { get; }

    /// <summary>
    /// Reads the saved settings into <see cref="Current"/>. A missing or unreadable file leaves
    /// the defaults in place, so a damaged settings file never stops the launcher starting.
    /// </summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes <paramref name="settings"/> current straight away, then saves them. A failed save is
    /// logged rather than thrown: the choice still applies until the launcher restarts.
    /// </summary>
    Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Keeps <see cref="LauncherSettings"/> in a small JSON file (see
/// <see cref="TvLauncherOptions.SettingsFilePath"/>), outside the install folder so updates
/// don't reset them.
/// </summary>
public sealed class LauncherSettingsStore(IOptions<TvLauncherOptions> options, ILogger<LauncherSettingsStore> logger)
    : ILauncherSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath = ResolveFilePath(options.Value.SettingsFilePath);

    public LauncherSettings Current { get; private set; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            await using var stream = File.OpenRead(_filePath);
            Current = await JsonSerializer.DeserializeAsync<LauncherSettings>(stream, JsonOptions, cancellationToken) ?? new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(ex, "Couldn't read launcher settings from {Path}; using the defaults", _filePath);
        }
    }

    public async Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default)
    {
        Current = settings;

        // Written beside the real file and then swapped in, so a crash mid-write can't leave a
        // half-written settings file behind.
        var temporaryPath = _filePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            await using (var stream = File.Create(temporaryPath))
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);

            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Couldn't save launcher settings to {Path}", _filePath);
        }
    }

    private static string ResolveFilePath(string configuredPath) =>
        string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoloNet", "TvLauncher", "settings.json")
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredPath));
}
