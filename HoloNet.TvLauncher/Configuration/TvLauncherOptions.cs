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
    /// Base URL of the HoloNet.Video catalog API, e.g. "http://videos.goa.no/api/v1/catalog".
    /// Used to fetch each library title's seasons, episodes and artwork — see
    /// <see cref="Services.IMediaCatalogApiClient"/>.
    /// </summary>
    public string VideoCatalogApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the HoloNet.Video library API, e.g. "http://videos.goa.no/api/v1/library" -
    /// the hand-picked Prime Video, Disney+ and HBO Max titles that fill each streaming folder. They're
    /// curated by hand rather than browsed live, because HoloNet.Video's providers only look a
    /// title up by id. Titles are added and removed from inside the launcher (see
    /// <see cref="Services.IMediaLibraryApiClient"/>).
    /// </summary>
    public string VideoLibraryApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the "Prime Video" folder tile is shown on the root screen at all. When
    /// <c>true</c>, the folder always appears (regardless of whether any library titles
    /// currently resolve, or whether the session cookie has
    /// expired) - its first tile is always the "Refresh Prime Login" action (see
    /// <see cref="Services.IPrimeAuthLauncher"/>), so you can fix an expired login without the
    /// folder disappearing on you first. Defaults to <c>false</c> since this whole integration
    /// is opt-in/best-effort.
    /// </summary>
    public bool PrimeVideoEnabled { get; set; } = false;

    /// <summary>
    /// Absolute path to the published <c>HoloNet.PrimeAuth.exe</c> (see
    /// <c>HoloNet.PrimeAuth/README.md</c>) - the standalone Playwright-based tool that opens a
    /// real browser window for you to log into Prime Video, then pushes the captured session
    /// cookie to <c>HoloNet.Video</c>. Launched as a child process by the picker's "Refresh Prime
    /// Login" tile (see <see cref="Services.IPrimeAuthLauncher"/>), the same way emulators are
    /// launched (see <see cref="Services.IGameLauncher"/>) - the picker just starts it and waits
    /// for it to exit.
    /// </summary>
    public string PrimeAuthToolPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the published <c>HoloNet.PrimePlayer.exe</c>, either absolute or relative to the
    /// launcher's own folder (releases bundle it next to the launcher) - the standalone Windows UI
    /// Automation-based tool that launches your real, everyday Firefox at a Prime Video title's
    /// deep link, clicks "Spill av"/"Fortsett" for you, and goes fullscreen (see
    /// <see cref="Services.IPrimePlayerLauncher"/>). Deliberately not Selenium/Playwright-driven:
    /// those control a specially-patched automation build of Firefox that lacks Widevine/EME
    /// (DRM) support, so playback could never actually start in it - this tool instead automates
    /// your real Firefox install (which already has Widevine provisioned, since manual playback
    /// there already works) purely via the same accessibility APIs screen readers use. When
    /// empty, or when launching it fails, selecting a movie/episode falls back to just opening
    /// the deep link in the default browser (see <see cref="Views.MainWindow.LaunchMediaAsync"/>).
    /// </summary>
    public string PrimePlayerToolPath { get; set; } = string.Empty;

    /// <summary>
    /// Whether the "Disney+" folder tile is shown on the root screen. Defaults to <c>false</c>,
    /// since, like Prime Video, this integration is opt-in.
    /// </summary>
    public bool DisneyPlusEnabled { get; set; } = false;

    /// <summary>
    /// Whether the "HBO Max" folder tile is shown on the root screen. Defaults to <c>false</c>,
    /// since, like the other streaming services, this integration is opt-in.
    /// </summary>
    public bool HboMaxEnabled { get; set; } = false;

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
    /// for the logical actions "Confirm", "Cancel", "Refresh", "Share", "Secondary", and "Tertiary". Each
    /// logical action maps to a list of candidate indices rather than a single one, because the
    /// same physical controller can report different HID button indices depending on connection
    /// type — e.g. a DualSense wired via USB vs. paired over Bluetooth. Any index in the list
    /// counts as a match, so both connection types work without needing separate configs or
    /// detection. Defaults cover the commonly observed USB DualSense layout (Cross=1, Circle=2,
    /// Triangle=3, Square=0, Options=9, Share=8) — add further indices here (e.g. from Bluetooth) if a
    /// specific controller/connection reports different numbers.
    /// </summary>
    public Dictionary<string, List<int>> DirectInputButtonMappings { get; set; } = new()
    {
        ["Confirm"] = [1],
        ["Cancel"] = [2],
        ["Refresh"] = [9],
        ["Share"] = [8],
        ["Secondary"] = [3],
        ["Tertiary"] = [0]
    };

    /// <summary>
    /// How long (in milliseconds) the quit combo — Options+Share on PlayStation pads,
    /// Start+Back on Xbox pads — must be held while a game is running before it quits the
    /// emulator and returns to the picker. Held (rather than a single press) deliberately, so
    /// it can't be triggered by accident.
    /// </summary>
    public int QuitHoldMilliseconds { get; set; } = 1500;

    /// <summary>
    /// Opt-in: when <c>true</c>, a "where I currently am" showcase screenshot is periodically
    /// captured while a game is running (see <see cref="Services.IGameScreenshotService"/>) and
    /// shown as the game's preview image in the picker. Defaults to <c>false</c> since capture
    /// relies on emulator-specific workarounds (e.g. PCSX2's own screenshot hotkey) that may not
    /// work, or may behave unexpectedly, for every emulator/game.
    /// </summary>
    public bool ShowcaseScreenshotEnabled { get; set; } = false;

    /// <summary>
    /// How often (in minutes) a "where I currently am" showcase screenshot is captured while a
    /// game is running (see <see cref="Services.IGameScreenshotService"/>). Captured on a timer
    /// rather than only at quit time, since the quit hold-combo shares its Start button with
    /// several emulators' own pause/menu overlay — capturing then would show that menu instead
    /// of actual gameplay. Only takes effect when <see cref="ShowcaseScreenshotEnabled"/> is
    /// <c>true</c>.
    /// </summary>
    public double ShowcaseScreenshotIntervalMinutes { get; set; } = 5;

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
    /// Where the choices made in the launcher's own Settings folder (e.g. which screensaver to
    /// show) are saved, so they survive restarts and updates. Defaults to
    /// <c>%LOCALAPPDATA%\HoloNet\TvLauncher\settings.json</c> when empty; environment variables
    /// are expanded. Unlike this file, those choices are made on the TV with the controller.
    /// </summary>
    public string SettingsFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Optional latitude, in decimal degrees (e.g. <c>58.97</c> for Stavanger), of the place
    /// whose weather the ambient clock screensaver shows. Leave this and
    /// <see cref="WeatherLongitude"/> empty to use this PC's own location from the Windows
    /// location service; set both to override it.
    /// </summary>
    public double? WeatherLatitude { get; set; }

    /// <summary>Longitude to pair with <see cref="WeatherLatitude"/>, e.g. <c>5.73</c> for Stavanger.</summary>
    public double? WeatherLongitude { get; set; }

    /// <summary>
    /// Optional place name shown next to the weather, e.g. <c>Stavanger</c>. When empty, the
    /// name is looked up from the location (see <see cref="ReverseGeocodingApiBaseUrl"/>).
    /// </summary>
    public string WeatherLocationName { get; set; } = string.Empty;

    /// <summary>
    /// BigDataCloud's free client-side reverse-geocoding endpoint, used to name the detected
    /// location. Needs no API key; only the location rounded to about a kilometre is sent.
    /// </summary>
    public string ReverseGeocodingApiBaseUrl { get; set; } = "https://api.bigdatacloud.net/data/reverse-geocode-client";

    /// <summary>
    /// The Open-Meteo forecast endpoint the weather is read from. Open-Meteo is free for
    /// non-commercial use and needs no API key.
    /// </summary>
    public string WeatherApiBaseUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";

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

    /// <summary>
    /// Non-game "open this URL" tiles shown alongside the game grid, e.g. links to
    /// <c>videos.goa.no</c>, <c>photos.goa.no</c>, or a streaming service. By default the URL
    /// opens in the default browser and nothing is tracked; tiles with
    /// <see cref="ShortcutMapping.Kiosk"/> set instead run as a managed fullscreen Firefox
    /// session that the quit combo returns from, like a game.
    /// </summary>
    public List<ShortcutMapping> Shortcuts { get; set; } = new();

    /// <summary>
    /// Absolute path to <c>firefox.exe</c>, used for kiosk shortcut tiles (see
    /// <see cref="ShortcutMapping.Kiosk"/>). Leave empty to auto-detect the standard
    /// <c>Program Files</c> install locations.
    /// </summary>
    public string FirefoxPath { get; set; } = string.Empty;

    /// <summary>
    /// Folder of the dedicated Firefox profile used for kiosk shortcut tiles (see
    /// <see cref="Services.IKioskBrowserLauncher"/>). Kept separate from your everyday profile so
    /// a kiosk session is always its own Firefox instance; log into streaming sites once inside
    /// it. Environment variables (e.g. <c>%LOCALAPPDATA%</c>) are expanded. Leave empty for
    /// <c>%LOCALAPPDATA%\HoloNet\TvLauncher\FirefoxKioskProfile</c>.
    /// </summary>
    public string KioskFirefoxProfilePath { get; set; } = string.Empty;

    /// <summary>
    /// Maps a game's raw <c>Platform</c> string (as returned by the Games API, e.g. "PS2") to a
    /// nicer display name shown on the platform folder tile in the picker (e.g. "PlayStation 2").
    /// Platforms not listed here just show their raw string as-is.
    /// </summary>
    public Dictionary<string, string> PlatformDisplayNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether TvLauncher checks GitHub Releases for a newer version on startup and, once
    /// downloaded, offers to install it (see <see cref="Services.IAppUpdateService"/>). Only
    /// takes effect for a Velopack-installed copy (i.e. installed via the release's Setup.exe);
    /// a portable/dev build silently skips the check entirely. Defaults to <c>true</c>.
    /// </summary>
    public bool AutoUpdateEnabled { get; set; } = true;

    /// <summary>
    /// GitHub repository URL used as the Velopack update feed, e.g.
    /// "https://github.com/bjorngoa2/HoloNet". Must match the repository that
    /// <c>vpk upload github</c> publishes releases to (see <c>.github/workflows/release.yml</c>).
    /// </summary>
    public string UpdateRepositoryUrl { get; set; } = "https://github.com/bjorngoa2/HoloNet";

    /// <summary>
    /// Opt-in: when <c>true</c>, gamepad input handling (raw HID reports, decoded button/D-pad
    /// state, and the UI's reaction to each button event) is logged to <c>gamepad-debug.log</c>
    /// next to the exe, for diagnosing controller issues with a specific pad/emulator
    /// combination — e.g. tracking down which HID report format a new controller or emulator
    /// switches into. Defaults to <c>false</c>; leave off for normal use, since it writes on
    /// every button press/poll tick and the log file is never rotated or capped. Turn on
    /// temporarily (via <c>TvLauncher__EnableGamepadDebugLogging=true</c> in Docker/env config,
    /// or directly in appsettings.json) only while actively reproducing an input problem.
    /// </summary>
    public bool EnableGamepadDebugLogging { get; set; } = false;
}

/// <summary>
/// A single non-game shortcut tile (see <see cref="TvLauncherOptions.Shortcuts"/>).
/// </summary>
public class ShortcutMapping
{
    /// <summary>
    /// Display title shown on the tile, e.g. "Videos".
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL opened when the tile is launched, e.g. "http://videos.goa.no".
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, the URL opens as a fullscreen "app": Firefox in kiosk mode (no tabs or
    /// address bar) with uBlock Origin (see <c>HoloNet.TvLauncher/Firefox/README.md</c>), and the
    /// picker waits for it like a game - hold the quit combo to return. When <c>false</c>
    /// (default), the URL just opens in the system default browser.
    /// </summary>
    public bool Kiosk { get; set; }

    /// <summary>
    /// Optional banner image URL (16:9 works best) for the tile. When not set, the tile shows
    /// <see cref="Icon"/> (or the title's initials) on <see cref="AccentColor"/>.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Optional built-in icon name for the tile, one of: Game, Video, Photo, Play, Folder, Key,
    /// Globe, Music (see <c>Views.TileIcons</c>). Unknown or empty falls back to initials.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Optional brand colour as hex, e.g. "#FF0033" for YouTube - used for the tile background,
    /// the home screen's ambient glow while the tile is focused, and the launch splash.
    /// Defaults to HoloNet blue.
    /// </summary>
    public string? AccentColor { get; set; }

    /// <summary>
    /// Optional one or two sentences shown on the home screen while the tile is focused.
    /// </summary>
    public string? Description { get; set; }
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
