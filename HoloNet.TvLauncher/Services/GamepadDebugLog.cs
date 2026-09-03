using System.IO;

namespace HoloNet.TvLauncher.Services;

/// <summary>
/// Opt-in diagnostic log shared by <see cref="GamepadInputService"/>, <see cref="RawInputGamepadReader"/>,
/// and <see cref="Views.MainWindow"/>, so the full chain — raw HID reports, the decoded
/// button/D-pad state, the logical <see cref="GamepadButton"/> event it produces, and the UI's
/// actual reaction to it — can be correlated by timestamp while diagnosing a controller issue
/// (e.g. a pad/emulator combination that switches HID report formats mid-session).
///
/// Gated by <see cref="Configuration.TvLauncherOptions.EnableGamepadDebugLogging"/> (set once,
/// at startup, via <see cref="Enabled"/>) rather than always writing, since this logs on every
/// button press/poll tick and the file is never rotated or size-capped — appropriate only while
/// actively reproducing an issue, not for routine use.
/// </summary>
internal static class GamepadDebugLog
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "gamepad-debug.log");

    /// <summary>
    /// Whether logging is active. Set once from <see cref="Configuration.TvLauncherOptions.EnableGamepadDebugLogging"/>
    /// during <see cref="GamepadInputService"/> construction, before any polling starts.
    /// </summary>
    public static bool Enabled { get; set; }

    public static void Log(string message)
    {
        if (!Enabled)
            return;

        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Best-effort diagnostics only — never let logging failures affect input handling.
        }
    }
}
