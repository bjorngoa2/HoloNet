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
    /// Per-game save-stats definitions (Bolts, playtime, etc.) shown as hover info on a game
    /// card in the picker, keyed by the game's <c>Title</c> exactly as returned by the Games
    /// API (case-insensitive). Keyed by title rather than <c>Id</c> because a game's <c>Id</c>
    /// is a Base64Url-encoded absolute file path (see HoloNet's file-identity convention) and
    /// is therefore unstable across machines/containers — titles are far more likely to stay
    /// constant. Reading real stats requires reverse-engineered, game-and-region-specific byte
    /// offsets, so only games explicitly configured here will show stats — everything else just
    /// shows the normal card with no hover info.
    /// </summary>
    public Dictionary<string, SaveStatsMapping> SaveStats { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Describes where to find a game's save data on a PS2-style memory card image, and which
/// byte offsets within the save file hold which stats. Currently PS2/PCSX2-specific — the
/// values here only make sense for a single game+region combination (offsets differ between
/// NTSC/US and PAL/EU releases of the same game, for example).
/// </summary>
public class SaveStatsMapping
{
    /// <summary>
    /// Absolute path to the PCSX2 memory card image (e.g. <c>Mcd001.ps2</c>) to read from.
    /// </summary>
    public string MemoryCardPath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the save directory on the memory card, e.g. <c>BESCES-50916RATCHET</c>. Visible
    /// via a memory card browser/tool such as mymc+.
    /// </summary>
    public string SaveDirectoryName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the individual save-slot file within the directory to read, e.g. <c>save0.bin</c>.
    /// PS2 games commonly store one file per save slot; only a single slot is read here
    /// (normally the most-recently-used one) since there's no live way to know which slot the
    /// player last used without also parsing <c>icon.sys</c>.
    /// </summary>
    public string SaveFileName { get; set; } = string.Empty;

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
