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
}
