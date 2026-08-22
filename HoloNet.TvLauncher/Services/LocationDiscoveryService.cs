using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace HoloNet.TvLauncher.Services;

public interface ILocationDiscoveryService
{
    /// <summary>
    /// Records that <paramref name="rawValue"/> was seen at <see cref="Configuration.SaveStatsMapping.LocationOffset"/>
    /// for <paramref name="gameTitle"/> but isn't in that game's <see cref="Configuration.SaveStatsMapping.LocationNames"/>
    /// map yet. A no-op if this exact (game, value) pair has already been recorded. Intended to
    /// be called every time a location value can't be translated to a name, so you can check
    /// <c>discovered-locations.json</c> later and add real names to appsettings.json for
    /// whichever new planets/levels you've reached since last checking.
    /// </summary>
    void RecordUnknownLocation(string gameTitle, int rawValue);
}

/// <summary>
/// Persists not-yet-named location/level IDs (see <see cref="Configuration.SaveStatsMapping.LocationOffset"/>)
/// to a small JSON file (<c>discovered-locations.json</c>, next to the exe) so they can be
/// manually named later — there's no generic way to know that Ratchet &amp; Clank's planet ID
/// "5" means "Rilgar" without either a documented reference (none exists) or actually being
/// there and telling the app what it's called, so this exists purely as a "here's what's new
/// since you last looked" worklist rather than attempting to guess names automatically.
/// </summary>
public class LocationDiscoveryService : ILocationDiscoveryService
{
    private const string FileName = "discovered-locations.json";

    private readonly ILogger<LocationDiscoveryService> _logger;
    private readonly Lock _lock = new();

    public LocationDiscoveryService(ILogger<LocationDiscoveryService> logger)
    {
        _logger = logger;
    }

    public void RecordUnknownLocation(string gameTitle, int rawValue)
    {
        lock (_lock)
        {
            try
            {
                var entries = Load();

                if (entries.TryGetValue(gameTitle, out var gameEntries)
                    && gameEntries.Any(e => e.Value == rawValue))
                    return; // Already recorded.

                gameEntries ??= [];
                gameEntries.Add(new DiscoveredLocation(rawValue, DateTime.UtcNow));
                entries[gameTitle] = gameEntries;

                Save(entries);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record discovered location {RawValue} for {GameTitle}",
                    rawValue, gameTitle);
            }
        }
    }

    private static Dictionary<string, List<DiscoveredLocation>> Load()
    {
        if (!File.Exists(FileName))
            return new Dictionary<string, List<DiscoveredLocation>>(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(FileName);
        return JsonSerializer.Deserialize<Dictionary<string, List<DiscoveredLocation>>>(json)
               ?? new Dictionary<string, List<DiscoveredLocation>>(StringComparer.OrdinalIgnoreCase);
    }

    private static void Save(Dictionary<string, List<DiscoveredLocation>> entries)
    {
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FileName, json);
    }

    private record DiscoveredLocation(int Value, DateTime FirstSeenUtc);
}
