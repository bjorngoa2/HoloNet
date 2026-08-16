namespace HoloNet.TvLauncher.Configuration;

/// <summary>
/// Typed configuration for the TV PC launcher client, bound from the "TvLauncher" section of
/// appsettings.json.
/// </summary>
public class TvLauncherOptions
{
    /// <summary>
    /// Base URL of the HoloNet.Games API, e.g. "http://games.goa.no/api/v1/games".
    /// </summary>
    public string GamesApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// How often (in milliseconds) the XInput gamepad state is polled.
    /// </summary>
    public int GamepadPollIntervalMs { get; set; } = 100;

    /// <summary>
    /// Left-stick deflection (0.0-1.0) required before it counts as a directional press.
    /// </summary>
    public double GamepadStickDeadzone { get; set; } = 0.5;

    /// <summary>
    /// Maps a game's <c>Platform</c> string (as returned by the Games API) to the emulator
    /// that should be launched for it.
    /// </summary>
    public Dictionary<string, EmulatorMapping> EmulatorMappings { get; set; } = new();

    /// <summary>
    /// DirectInput button indices (used for non-XInput pads such as PS4/PS5 DualShock/DualSense)
    /// for the logical actions "Confirm", "Cancel", "Refresh", and "Share". Defaults match the
    /// common PS4/PS5 DirectInput HID report layout (Cross=1, Circle=2, Options=9, Share=8) —
    /// override here if a specific controller numbers its buttons differently.
    /// </summary>
    public Dictionary<string, int> DirectInputButtonMappings { get; set; } = new()
    {
        ["Confirm"] = 1,
        ["Cancel"] = 2,
        ["Refresh"] = 9,
        ["Share"] = 8
    };

    /// <summary>
    /// How long (in milliseconds) the quit combo — Options+Share on PlayStation pads,
    /// Start+Back on Xbox pads — must be held while a game is running before it quits the
    /// emulator and returns to the picker. Held (rather than a single press) deliberately, so
    /// it can't be triggered by accident.
    /// </summary>
    public int QuitHoldMilliseconds { get; set; } = 1500;

    /// <summary>
    /// How many minutes of no gamepad/keyboard input before a burn-in-protection screensaver
    /// takes over the picker screen (see <see cref="ScreensaverEnabled"/>). This exists because
    /// the picker is a static, bright grid of cards left on-screen for potentially hours at a
    /// time on a TV — a real burn-in risk on OLED panels in particular. Any input immediately
    /// dismisses it and resets the idle timer.
    /// </summary>
    public double ScreensaverIdleMinutes { get; set; } = 5;

    /// <summary>
    /// Whether the idle screensaver is enabled at all. Defaults to <c>true</c>; set to
    /// <c>false</c> to disable entirely (e.g. on a non-OLED display where burn-in isn't a
    /// concern).
    /// </summary>
    public bool ScreensaverEnabled { get; set; } = true;

    /// <summary>
    /// Absolute paths to every PCSX2 memory card image (e.g. <c>Mcd001.ps2</c>, <c>Mcd002.ps2</c>)
    /// to scan for save data. Every save directory found on these cards is auto-matched to a
    /// game by its <c>icon.sys</c> title (see <see cref="SaveStats"/> for how to add
    /// game-specific currency/playtime stats on top of the auto-discovered match) — no manual
    /// per-game directory/file configuration is required just to get "last played" working.
    /// </summary>
    public List<string> MemoryCardPaths { get; set; } = new();

    /// <summary>
    /// Per-game save-stats definitions (Bolts, playtime, etc.) shown as hover info on a game
    /// card in the picker, keyed by the game's <c>Title</c> exactly as returned by the Games
    /// API (case-insensitive). Keyed by title rather than <c>Id</c> because a game's <c>Id</c>
    /// is a Base64Url-encoded absolute file path (see HoloNet's file-identity convention) and
    /// is therefore unstable across machines/containers — titles are far more likely to stay
    /// constant. Only needed for games where you want currency/playtime shown — those require
    /// reverse-engineered, game-and-region-specific byte offsets that can't be auto-discovered.
    /// "Last played" and the save directory/file to read from are auto-discovered from
    /// <see cref="MemoryCardPaths"/> by matching the save's on-card <c>icon.sys</c> title
    /// against the game's title (see <see cref="SaveStatsMapping.SaveDirectoryName"/> and
    /// <see cref="SaveStatsMapping.SaveFileName"/> to override that auto-match if it's wrong or
    /// ambiguous for a specific game).
    /// </summary>
    public Dictionary<string, SaveStatsMapping> SaveStats { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Optional per-game overrides/extras on top of the auto-discovered save match (see
/// <see cref="TvLauncherOptions.MemoryCardPaths"/> and <see cref="TvLauncherOptions.SaveStats"/>).
/// Every field here is optional: leave the whole entry out entirely and a game still gets
/// generic "last played" info for free (if a matching save is found by title); add an entry
/// only to supply currency/playtime offsets, or to override the auto-match if it picks the
/// wrong save (e.g. two games/regions share a very similar on-card title).
/// </summary>
public class SaveStatsMapping
{
    /// <summary>
    /// Absolute path to a specific PCSX2 memory card image to read from, overriding the
    /// auto-scan across all of <see cref="TvLauncherOptions.MemoryCardPaths"/>. Only needed if
    /// a game's save needs to be pinned to one specific card (e.g. when the same game exists on
    /// two different cards for two different players).
    /// </summary>
    public string? MemoryCardPath { get; set; }

    /// <summary>
    /// Name of the save directory on the memory card, e.g. <c>BESCES-50916RATCHET</c>,
    /// overriding the auto-match-by-<c>icon.sys</c>-title lookup. Only needed if the automatic
    /// title match is wrong or ambiguous for this game. Visible via a memory card browser/tool
    /// such as mymc+.
    /// </summary>
    public string? SaveDirectoryName { get; set; }

    /// <summary>
    /// Name of the individual save-slot file within the directory to read, e.g. <c>save0.bin</c>,
    /// overriding the auto-picked most-recently-modified file in the matched directory. PS2
    /// games commonly store one file per save slot; only needed if the most-recently-modified
    /// file isn't the one that actually holds the stats (e.g. a game keeps its counters in a
    /// different, less-frequently-written file within the same save directory).
    /// </summary>
    public string? SaveFileName { get; set; }

    /// <summary>
    /// Byte offset (little-endian uint32) within the save file where the currency/points value
    /// is stored, e.g. Ratchet &amp; Clank's "Bolts" at offset 0x24. Null if not applicable/known.
    /// </summary>
    public int? CurrencyOffset { get; set; }

    /// <summary>
    /// Display label for the value at <see cref="CurrencyOffset"/>, e.g. "Bolts".
    /// </summary>
    public string CurrencyLabel { get; set; } = "Currency";

    /// <summary>
    /// Byte offset (little-endian uint32) within the save file where a frame-count playtime
    /// counter is stored, e.g. offset 0x3c for Ratchet &amp; Clank. Null if not applicable/known.
    /// </summary>
    public int? PlaytimeFramesOffset { get; set; }

    /// <summary>
    /// Frame rate used to convert <see cref="PlaytimeFramesOffset"/>'s raw frame count into a
    /// duration. PAL games run at 50fps, NTSC at 60fps.
    /// </summary>
    public double PlaytimeFrameRate { get; set; } = 50.0;

    /// <summary>
    /// Byte offset (little-endian int32) within the save file where a location/level index is
    /// stored, e.g. Ratchet &amp; Clank's current-planet ID. Null if not applicable/known.
    /// Combined with <see cref="LocationNames"/> to show a human-readable name; there's no way
    /// to auto-discover what a raw numeric ID actually means (it requires knowing the specific
    /// game's internal level ordering), so unmapped IDs are recorded to
    /// <c>discovered-locations.json</c> (see <see cref="LocationDiscoveryService"/>) for you to
    /// name later, rather than guessed at.
    /// </summary>
    public int? LocationOffset { get; set; }

    /// <summary>
    /// Maps a raw value read from <see cref="LocationOffset"/> to a human-readable name, e.g.
    /// <c>{ "1": "Novalis", "3": "Kerwan" }</c>. Build this up over time as you play — any ID
    /// encountered that isn't in this map yet is auto-recorded (with a timestamp) to
    /// <c>discovered-locations.json</c> next to the exe, so you don't have to manually
    /// reverse-engineer or remember which raw numbers you've already seen.
    /// </summary>
    public Dictionary<int, string> LocationNames { get; set; } = new();
}

/// <summary>
/// Describes how to launch a specific emulator for a platform.
/// </summary>
public class EmulatorMapping
{
    /// <summary>
    /// Absolute path to the emulator executable on the TV PC.
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Command-line arguments template. The token <c>{NetworkPath}</c> is replaced with the
    /// game's network share path before launching.
    /// </summary>
    public string ArgumentsTemplate { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, appends <see cref="HideWindowArgument"/> to the launch arguments so
    /// the emulator hides its own window/UI while running (e.g. PCSX2's <c>-nogui</c>) —
    /// only the game itself is shown, with no menu/taskbar window flashing up first. Defaults
    /// to <c>false</c> (emulator shows its normal window).
    /// </summary>
    public bool HideWindow { get; set; }

    /// <summary>
    /// The command-line flag appended when <see cref="HideWindow"/> is <c>true</c>. Defaults
    /// to PCSX2's <c>-nogui</c>; override per-emulator if a different one uses a different
    /// flag for the same behavior.
    /// </summary>
    public string HideWindowArgument { get; set; } = "-nogui";

    /// <summary>
    /// When <c>true</c>, the quit combo kills this emulator's process immediately instead of
    /// requesting a graceful close. Intended for emulators launched hidden (e.g. PCSX2's
    /// <c>-nogui</c>), where a graceful close would otherwise pop up a visible confirmation
    /// dialog that the player can't interact with on the hidden window. Defaults to
    /// <c>false</c> (graceful close, so a visible emulator can show its own prompts).
    /// </summary>
    public bool ForceKillOnQuit { get; set; }
}
