using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Presses a web player's own keyboard shortcuts in the foreground browser. Some streaming sites
/// (e.g. Disney+) publish play state but no timeline to the system media session, so there is no
/// position to seek to; their players all skip 10 seconds on the arrow keys, though, and the
/// browser keeps keyboard focus under the click-through playback controls.
/// </summary>
internal static class PlayerKeys
{
    private const ushort LeftArrowKey = 0x25;
    private const ushort RightArrowKey = 0x27;
    private const uint KeyboardInput = 1;
    private const uint ExtendedKey = 0x0001;
    private const uint KeyUp = 0x0002;

    /// <summary>
    /// Skips back (<paramref name="direction"/> &lt; 0) or ahead one step. Returns <c>false</c>
    /// when the foreground window is the launcher's own, which has no player to send keys to.
    /// </summary>
    public static bool Skip(int direction)
    {
        var foreground = GetForegroundWindow();
        GetWindowThreadProcessId(foreground, out var processId);
        if (foreground == IntPtr.Zero || processId == Environment.ProcessId)
            return false;

        var key = direction < 0 ? LeftArrowKey : RightArrowKey;
        Input[] inputs = [KeyInput(key, ExtendedKey), KeyInput(key, ExtendedKey | KeyUp)];
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length)
            Debug.WriteLine($"SendInput sent {sent} of {inputs.Length} key events (error {Marshal.GetLastWin32Error()}).");

        return sent == inputs.Length;
    }

    private static Input KeyInput(ushort virtualKey, uint flags) => new()
    {
        Type = KeyboardInput,
        Keyboard = new KeyboardInputData { VirtualKey = virtualKey, Flags = flags }
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public KeyboardInputData Keyboard;

        // The native INPUT is a union whose largest member is MOUSEINPUT.
        private readonly long _mouseInputPadding;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputData
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
}
