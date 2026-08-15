using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public interface ISaveStatsService
{
    /// <summary>
    /// Returns save-file stats (Bolts, playtime, etc.) for the given game title, or <c>null</c>
    /// if the game has no <see cref="SaveStatsMapping"/> configured, or the memory
    /// card/save/file couldn't be read.
    /// </summary>
    SaveStats? GetStats(string gameTitle);
}

/// <summary>
/// Reads game-specific stats out of a PCSX2 memory card save file, using the byte offsets
/// configured per-game in <see cref="TvLauncherOptions.SaveStats"/>. See
/// <see cref="Ps2MemoryCardReader"/> for how the memory card image itself is parsed.
///
/// This only supports the handful of stats/offsets that have been manually reverse-engineered
/// for specific games (see the Ratchet &amp; Clank example in appsettings.json) — there's no
/// generic PS2 save format, so every new game needs its own offsets figured out and added to
/// config before stats will show for it.
/// </summary>
public class SaveStatsService(IOptions<TvLauncherOptions> options, ILogger<SaveStatsService> logger)
    : ISaveStatsService
{
    private readonly TvLauncherOptions _options = options.Value;

    public SaveStats? GetStats(string gameTitle)
    {
        if (!_options.SaveStats.TryGetValue(gameTitle, out var mapping))
            return null;

        if (string.IsNullOrWhiteSpace(mapping.MemoryCardPath)
            || string.IsNullOrWhiteSpace(mapping.SaveDirectoryName)
            || string.IsNullOrWhiteSpace(mapping.SaveFileName))
            return null;

        byte[]? data;
        try
        {
            data = Ps2MemoryCardReader.ReadFile(mapping.MemoryCardPath, mapping.SaveDirectoryName, mapping.SaveFileName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read save stats for game {GameTitle} from {MemoryCardPath}",
                gameTitle, mapping.MemoryCardPath);
            return null;
        }

        if (data is null)
            return null;

        int? currency = null;
        if (mapping.CurrencyOffset is { } currencyOffset && currencyOffset + 4 <= data.Length)
            currency = BitConverter.ToInt32(data, currencyOffset);

        TimeSpan? playtime = null;
        if (mapping.PlaytimeFramesOffset is { } playtimeOffset && playtimeOffset + 4 <= data.Length)
        {
            var frames = BitConverter.ToUInt32(data, playtimeOffset);
            var seconds = frames / mapping.PlaytimeFrameRate;
            playtime = TimeSpan.FromSeconds(seconds);
        }

        if (currency is null && playtime is null)
            return null;

        return new SaveStats(currency, mapping.CurrencyLabel, playtime);
    }
}
