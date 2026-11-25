using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Win32 plumbing for launcher windows that float over a fullscreen browser (the launch curtain
/// and the playback controls). They're click-through and never take focus, so the browser keeps
/// receiving input and stays fullscreen; being layered windows also keeps the browser from
/// treating itself as covered (which would mark the page hidden and stall playback).
/// </summary>
internal static class FloatingWindows
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const uint MonitorDefaultToNearest = 2;

    /// <summary>
    /// Sizes <paramref name="window"/> to cover <paramref name="reference"/>'s monitor. WPF can't
    /// show a non-activating window maximized, so the bounds are set explicitly.
    /// </summary>
    public static void CoverMonitorOf(Window window, Window reference)
    {
        var monitor = MonitorBoundsOf(reference);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = monitor.Left;
        window.Top = monitor.Top;
        window.Width = monitor.Width;
        window.Height = monitor.Height;
    }

    /// <summary>Makes <paramref name="window"/> click-through, non-activating and absent from Alt+Tab. Call once its handle exists.</summary>
    public static void MakeClickThrough(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var exStyle = GetWindowLong(handle, GwlExStyle);
        SetWindowLong(handle, GwlExStyle, exStyle | WsExTransparent | WsExToolWindow | WsExNoActivate);
    }

    /// <summary>The bounds of the monitor showing <paramref name="window"/>, in device-independent pixels.</summary>
    private static Rect MonitorBoundsOf(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        GetMonitorInfo(MonitorFromWindow(handle, MonitorDefaultToNearest), ref info);

        var fromDevice = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var bounds = info.Monitor;
        return new Rect(
            fromDevice.Transform(new Point(bounds.Left, bounds.Top)),
            fromDevice.Transform(new Point(bounds.Right, bounds.Bottom)));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
