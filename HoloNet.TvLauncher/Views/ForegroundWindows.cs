using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Brings a launcher window back in front of an app that's still running (e.g. the browser left
/// paused when the viewer returns home). Windows' foreground lock stops a background process from
/// activating itself, and controller input doesn't count as user input to the launcher, so the
/// launcher briefly shares the foreground thread's input state, which lets it take the foreground.
/// </summary>
internal static class ForegroundWindows
{
    public static void TakeForeground(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var foreground = GetForegroundWindow();
        if (handle == IntPtr.Zero || handle == foreground)
            return;

        var ownThread = GetCurrentThreadId();
        var foregroundThread = GetWindowThreadProcessId(foreground, out _);
        var attached = foregroundThread != 0
            && foregroundThread != ownThread
            && AttachThreadInput(ownThread, foregroundThread, true);
        try
        {
            BringWindowToTop(handle);
            SetForegroundWindow(handle);
        }
        finally
        {
            if (attached)
                AttachThreadInput(ownThread, foregroundThread, false);
        }

        window.Activate();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
