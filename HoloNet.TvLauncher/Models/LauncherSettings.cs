namespace HoloNet.TvLauncher.Models;

/// <summary>Which idle screensaver the launcher shows (see <see cref="LauncherSettings"/>).</summary>
public enum ScreensaverStyle
{
    /// <summary>A large dimmed clock, the date and the current weather, moving now and then.</summary>
    AmbientClock,

    /// <summary>A small dim HoloNet logo drifting and bouncing off the screen edges.</summary>
    BouncingLogo
}

/// <summary>
/// The choices made in the launcher's Settings folder, saved by
/// <see cref="Services.ILauncherSettingsStore"/>. Settings that only make sense to change on the
/// TV live here; everything else stays in <c>appsettings.json</c>.
/// </summary>
/// <param name="HomeOrder">
/// The home tiles' keys in the order the player arranged them, or <c>null</c> for the default
/// order (see <see cref="Views.HomeOrder"/>).
/// </param>
public sealed record LauncherSettings(
    ScreensaverStyle Screensaver = ScreensaverStyle.AmbientClock,
    IReadOnlyList<string>? HomeOrder = null);
