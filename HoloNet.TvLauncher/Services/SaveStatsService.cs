using System.Text;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Services;

public interface ISaveStatsService
{
    /// <summary>
    /// Returns save-file stats (last played, and Bolts/playtime if configured) for the given
    /// game title, or <c>null</c> if no matching save could be auto-discovered on any of the
    /// configured memory cards.
    /// </summary>
    SaveStats? GetStats(string gameTitle);
}

/// <summary>
/// Reads game stats out of PS2/PCSX2 memory card images. Two things are auto-discovered
/// generically for every game, from any card listed in <see cref="TvLauncherOptions.MemoryCardPaths"/>,
/// with no per-game configuration required: which on-card save directory belongs to the game
/// (matched by <c>icon.sys</c> title vs. the game's own <c>Title</c> — see
/// <see cref="Ps2MemoryCardReader.ListSaves"/>) and its "last played" timestamp (the newest
/// directory-entry modified time across the save's files). Currency/playtime, on the other
/// hand, can't be made generic — every game stores them at different byte offsets in a
/// different save-file layout — so those only show up for games with a
/// <see cref="SaveStatsMapping"/> entry configured in <see cref="TvLauncherOptions.SaveStats"/>.
/// </summary>
public class SaveStatsService(IOptions<TvLauncherOptions> options, ILogger<SaveStatsService> logger,
    ILocationDiscoveryService locationDiscoveryService) : ISaveStatsService
{
    private readonly TvLauncherOptions _options = options.Value;

    public SaveStats? GetStats(string gameTitle)
    {
        _options.SaveStats.TryGetValue(gameTitle, out var mapping);

        var match = FindMatchingSave(gameTitle, mapping);
        if (match is null)
            return null;

        var (memoryCardPath, entry) = match.Value;
        var fileName = mapping?.SaveFileName ?? entry.MostRecentFileName;

        byte[]? data = null;
        DateTime? lastPlayed = entry.LastModified;
        if (fileName is not null && mapping is { CurrencyOffset: not null } or { PlaytimeFramesOffset: not null }
                or { LocationOffset: not null })
        {
            try
            {
                var result = Ps2MemoryCardReader.ReadFileWithMetadata(memoryCardPath, entry.DirectoryName, fileName);
                data = result?.Data;
                lastPlayed ??= result?.Modified;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read save stats for game {GameTitle} from {MemoryCardPath}",
                    gameTitle, memoryCardPath);
            }
        }

        int? currency = ReadInt32OrNull(data, mapping?.CurrencyOffset);

        TimeSpan? playtime = null;
        if (ReadUInt32OrNull(data, mapping?.PlaytimeFramesOffset) is { } frames)
            playtime = TimeSpan.FromSeconds(frames / mapping!.PlaytimeFrameRate);

        string? location = null;
        if (ReadInt32OrNull(data, mapping?.LocationOffset) is { } rawLocation)
        {
            if (mapping!.LocationNames.TryGetValue(rawLocation, out var name))
            {
                location = name;
            }
            else
            {
                // Show the raw ID rather than nothing at all — better to see "something changed
                // here, I just don't have a name for it yet" than for the location to silently
                // disappear once you reach a new, not-yet-named area.
                location = $"Unknown ({rawLocation})";
                locationDiscoveryService.RecordUnknownLocation(gameTitle, rawLocation);
            }
        }

        if (currency is null && playtime is null && lastPlayed is null && location is null)
            return null;

        return new SaveStats(currency, mapping?.CurrencyLabel ?? "Currency", playtime, lastPlayed, location);
    }

    /// <summary>
    /// Reads a little-endian <see cref="int"/> at <paramref name="offset"/> within
    /// <paramref name="data"/>, or <c>null</c> if <paramref name="data"/> hasn't been read,
    /// <paramref name="offset"/> isn't configured, or the offset would read past the end of the
    /// save file (a mismatched/wrong offset for this save's actual layout).
    /// </summary>
    private static int? ReadInt32OrNull(byte[]? data, int? offset) =>
        data is not null && offset is { } o && o + 4 <= data.Length
            ? BitConverter.ToInt32(data, o)
            : null;

    /// <summary>Unsigned counterpart to <see cref="ReadInt32OrNull"/>, e.g. for frame counts.</summary>
    private static uint? ReadUInt32OrNull(byte[]? data, int? offset) =>
        data is not null && offset is { } o && o + 4 <= data.Length
            ? BitConverter.ToUInt32(data, o)
            : null;

    /// <summary>
    /// Finds the on-card save that belongs to <paramref name="gameTitle"/>: either the
    /// explicit <see cref="SaveStatsMapping.MemoryCardPath"/>/<see cref="SaveStatsMapping.SaveDirectoryName"/>
    /// override if configured, or an auto-match by comparing every card's save titles (from
    /// <c>icon.sys</c>) against the game's own title. Multiple directories can legitimately
    /// title-match the same game (e.g. separate NTSC/PAL saves from testing different region
    /// ISOs of the same game) — when that happens, the most-recently-modified one is preferred,
    /// since that's the save the player actually cares about right now, and (for games with
    /// currency/playtime configured) it avoids silently reading the wrong region's save with
    /// offsets that don't apply to it.
    /// </summary>
    private (string MemoryCardPath, Ps2MemoryCardReader.Ps2SaveEntry Entry)? FindMatchingSave(string gameTitle,
        SaveStatsMapping? mapping)
    {
        var cardPaths = mapping?.MemoryCardPath is { Length: > 0 } explicitPath
            ? [explicitPath]
            : _options.MemoryCardPaths;

        (string CardPath, Ps2MemoryCardReader.Ps2SaveEntry Entry)? best = null;

        foreach (var cardPath in cardPaths)
        {
            IReadOnlyList<Ps2MemoryCardReader.Ps2SaveEntry> saves;
            try
            {
                saves = Ps2MemoryCardReader.ListSaves(cardPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to scan memory card {MemoryCardPath} for saves", cardPath);
                continue;
            }

            var candidates = mapping?.SaveDirectoryName is { Length: > 0 } explicitDir
                ? saves.Where(s => string.Equals(s.DirectoryName, explicitDir, StringComparison.OrdinalIgnoreCase))
                : saves.Where(s => TitlesMatch(gameTitle, s.Title));

            foreach (var candidate in candidates)
            {
                if (best is null || (candidate.LastModified ?? DateTime.MinValue) > (best.Value.Entry.LastModified ?? DateTime.MinValue))
                    best = (cardPath, candidate);
            }
        }

        return best;
    }

    /// <summary>
    /// Loosely compares a game's API title against a save's on-card <c>icon.sys</c> title.
    /// PS2 save titles are frequently abbreviated/reformatted compared to a game's "real" title
    /// (e.g. "Ratchet &amp; Clank" vs. the API's "Ratchet and Clank", 2-line titles collapsed
    /// with a space, region suffixes, connector words dropped) so neither an exact match nor a
    /// simple substring check is reliable — instead this splits both into significant words
    /// (dropping common connectors like "and"/"the"/"of"/"&amp;" and normalizing fullwidth
    /// Unicode characters, which PS2 save titles commonly use, down to their ASCII equivalents
    /// via NFKC) and matches if most of one title's words appear in the other.
    /// </summary>
    private static bool TitlesMatch(string gameTitle, string? saveTitle)
    {
        if (string.IsNullOrWhiteSpace(saveTitle))
            return false;

        var gameWords = SignificantWords(gameTitle);
        var saveWords = SignificantWords(saveTitle);
        if (gameWords.Count == 0 || saveWords.Count == 0)
            return false;

        var smaller = gameWords.Count <= saveWords.Count ? gameWords : saveWords;
        var larger = gameWords.Count <= saveWords.Count ? saveWords : gameWords;
        var matchingWords = smaller.Count(w => larger.Any(l => l.Contains(w) || w.Contains(l)));

        // Require most of the shorter title's significant words to appear in the other title —
        // exact fraction chosen so a single very short/generic word overlap ("the") can't count
        // as a match, but real title matches (which typically share almost every word) pass.
        return matchingWords >= Math.Max(1, (int)Math.Ceiling(smaller.Count * 0.6));
    }

    private static readonly HashSet<string> ConnectorWords = new(StringComparer.Ordinal)
        { "and", "the", "of", "a", "an" };

    private static HashSet<string> SignificantWords(string title) =>
        title.Normalize(NormalizationForm.FormKC)
            .Split(' ', '\t', '\u3000', '-', ':', ',')
            .Select(w => new string(w.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray()))
            .Where(w => w.Length > 0 && !ConnectorWords.Contains(w))
            .ToHashSet();
}
