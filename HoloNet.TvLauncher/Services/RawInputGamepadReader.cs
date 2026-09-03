using System.IO;
using System.Runtime.InteropServices;

namespace HoloNet.TvLauncher.Services;

/// <summary>
/// Reads gamepad/joystick button and D-pad (hat switch) state via the Windows <c>Raw Input</c>
/// API (<c>RegisterRawInputDevices</c>/<c>WM_INPUT</c>) instead of DirectInput.
///
/// This exists because DirectInput (SharpDX) cannot parse the Bluetooth-connected DualSense's
/// "extended" HID report format (report ID <c>0x31</c>, used for gyro/touchpad/adaptive
/// triggers) — confirmed via direct testing where even PCSX2's own DirectInput binding page
/// detected zero input from the same pad. Once something (SDL, used internally by PCSX2)
/// switches the pad into that extended mode, it stays there — even after that app closes —
/// until the pad is fully power-cycled over Bluetooth. Raw Input reads through the OS's own
/// HID class driver and its already-parsed report descriptor (<c>HidP_GetUsages</c>), so it
/// correctly understands whichever report format is currently active, extended or not.
///
/// Runs *alongside* DirectInput (see <see cref="GamepadInputService"/>), not instead of it —
/// their button states are OR'd together each poll, so this only adds a fallback signal and
/// never regresses a case where DirectInput already works fine (e.g. wired pads).
///
/// Deliberately covers only buttons and the D-pad hat switch, not analog sticks — Raw Input's
/// value-usage scaling needs each device's declared logical range to normalize correctly, which
/// adds real complexity for a signal that, unlike buttons/D-pad, DirectInput already provides
/// reliably enough in practice. Button identification reuses
/// <see cref="Configuration.TvLauncherOptions.DirectInputButtonMappings"/> (same assumption as
/// the earlier, since-reverted quit-combo-only attempt: HID button Usage IDs are 1-based and
/// align with DirectInput's 0-based button array indices) — this time with debug logging (see
/// <see cref="LogPath"/>) so that assumption can actually be verified against live hardware
/// instead of shipping unverified again.
/// </summary>
internal sealed class RawInputGamepadReader : IDisposable
{
    private const int WmInput = 0x00FF;
    private const uint RidInput = 0x10000003;
    private const uint RidiPreparsedData = 0x20000005;
    private const uint RidevInputSink = 0x00000100;
    private const uint RimTypeHid = 2;
    private const ushort HidUsagePageGeneric = 0x01;
    private const ushort HidUsageGamepad = 0x05;
    private const ushort HidUsageJoystick = 0x04;
    private const ushort HidUsagePageButton = 0x09;
    private const ushort HidUsageHatSwitch = 0x39;
    private const int HidPInput = 0;
    private const int HidPStatusSuccess = 0x00110000;
    private const int MaxTrackedButtons = 32;

    /// <summary>Log file next to the exe — deliberately verbose-but-deduped, for one-off diagnosis.</summary>
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "rawinput-debug.log");

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawHid
    {
        public uint SizeHid;
        public uint Count;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RawInputDevice[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern int HidP_GetUsages(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        [Out] ushort[] usageList,
        ref uint usageLength,
        IntPtr preparsedData,
        byte[] report,
        uint reportLength);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern int HidP_GetUsageValue(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        ushort usage,
        out uint usageValue,
        IntPtr preparsedData,
        byte[] report,
        uint reportLength);

    private readonly Dictionary<IntPtr, IntPtr> _preparsedDataByDevice = new();

    private bool[] _lastLoggedButtons = [];
    private int _lastLoggedPov = int.MinValue;

    /// <summary>Current button state, indexed to match <see cref="Configuration.TvLauncherOptions.DirectInputButtonMappings"/>. Never null; all-false when no data has arrived yet.</summary>
    public bool[] Buttons { get; private set; } = new bool[MaxTrackedButtons];

    /// <summary>Hat switch direction in hundredths of a degree (0=Up, 9000=Right, 18000=Down, 27000=Left), or -1 if centered/not present on this device's current report.</summary>
    public int Pov { get; private set; } = -1;

    /// <summary>Whether at least one valid Raw Input HID report has been successfully parsed since <see cref="Attach"/>.</summary>
    public bool HasReceivedData { get; private set; }

    public void Attach(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return;

        var devices = new[]
        {
            new RawInputDevice { UsagePage = HidUsagePageGeneric, Usage = HidUsageGamepad, Flags = RidevInputSink, Target = windowHandle },
            new RawInputDevice { UsagePage = HidUsagePageGeneric, Usage = HidUsageJoystick, Flags = RidevInputSink, Target = windowHandle }
        };

        var registered = RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>());
        TryLog($"Attach windowHandle=0x{windowHandle:X} registered={registered} lastError={(registered ? 0 : Marshal.GetLastWin32Error())}");
    }

    public void HandleWindowMessage(int msg, IntPtr lParam)
    {
        if (msg != WmInput)
            return;

        try
        {
            ProcessRawInput(lParam);
        }
        catch (Exception ex)
        {
            // Defensive: a malformed/unexpected raw input payload must never crash the picker's
            // message loop — this is a best-effort fallback signal, not a critical path.
            TryLog($"ProcessRawInput threw: {ex}");
        }
    }

    private void ProcessRawInput(IntPtr hRawInput)
    {
        var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        uint size = 0;

        if (GetRawInputData(hRawInput, RidInput, IntPtr.Zero, ref size, headerSize) == unchecked((uint)-1) || size == 0)
            return;

        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetRawInputData(hRawInput, RidInput, buffer, ref size, headerSize) != size)
                return;

            var header = Marshal.PtrToStructure<RawInputHeader>(buffer);
            if (header.Type != RimTypeHid)
                return;

            var rawHidOffset = (int)headerSize;
            var rawHid = Marshal.PtrToStructure<RawHid>(IntPtr.Add(buffer, rawHidOffset));
            if (rawHid.Count == 0 || rawHid.SizeHid == 0)
                return;

            var reportLength = (int)rawHid.SizeHid;
            var reportsOffset = rawHidOffset + Marshal.SizeOf<RawHid>();
            // Multiple reports can be coalesced into one WM_INPUT if the device outpaces our
            // message loop; only the most recent one reflects current button state.
            var latestReportOffset = reportsOffset + (int)(rawHid.Count - 1) * reportLength;

            var report = new byte[reportLength];
            Marshal.Copy(IntPtr.Add(buffer, latestReportOffset), report, 0, reportLength);

            var preparsedData = GetOrFetchPreparsedData(header.Device);
            if (preparsedData == IntPtr.Zero)
                return;

            var usageList = new ushort[MaxTrackedButtons];
            var usageLength = (uint)usageList.Length;
            var buttonStatus = HidP_GetUsages(
                HidPInput, HidUsagePageButton, 0, usageList, ref usageLength, preparsedData, report, (uint)report.Length);

            var buttons = new bool[MaxTrackedButtons];
            if (buttonStatus == HidPStatusSuccess)
            {
                for (var i = 0; i < usageLength; i++)
                {
                    var index = usageList[i] - 1; // HID usage IDs are 1-based; convert to a 0-based index.
                    if (index >= 0 && index < buttons.Length)
                        buttons[index] = true;
                }
            }

            var povStatus = HidP_GetUsageValue(
                HidPInput, HidUsagePageGeneric, 0, HidUsageHatSwitch, out var hatValue, preparsedData, report, (uint)report.Length);

            // Standard HID hat switches report 0-7 for the eight directions and a "null state"
            // value (commonly 8) when centered; anything outside 0-7 is treated as centered.
            var pov = povStatus == HidPStatusSuccess && hatValue <= 7 ? (int)hatValue * 4500 : -1;

            Buttons = buttons;
            Pov = pov;
            HasReceivedData = true;

            LogIfChanged(buttons, pov);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void LogIfChanged(bool[] buttons, int pov)
    {
        if (pov == _lastLoggedPov && buttons.AsSpan().SequenceEqual(_lastLoggedButtons))
            return;

        _lastLoggedPov = pov;
        _lastLoggedButtons = (bool[])buttons.Clone();

        var pressedIndices = string.Join(",", Enumerable.Range(0, buttons.Length).Where(i => buttons[i]));
        TryLog($"state pov={pov} pressed=[{pressedIndices}]");
    }

    private IntPtr GetOrFetchPreparsedData(IntPtr device)
    {
        if (_preparsedDataByDevice.TryGetValue(device, out var cached))
            return cached;

        uint size = 0;
        GetRawInputDeviceInfo(device, RidiPreparsedData, IntPtr.Zero, ref size);
        if (size == 0)
            return IntPtr.Zero;

        var buffer = Marshal.AllocHGlobal((int)size);
        if (GetRawInputDeviceInfo(device, RidiPreparsedData, buffer, ref size) == unchecked((uint)-1))
        {
            Marshal.FreeHGlobal(buffer);
            return IntPtr.Zero;
        }

        _preparsedDataByDevice[device] = buffer;
        TryLog($"Registered preparsed data for device=0x{device:X}");
        return buffer;
    }

    private static void TryLog(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Best-effort diagnostics only — never let logging failures affect input handling.
        }
    }

    public void Dispose()
    {
        foreach (var preparsedData in _preparsedDataByDevice.Values)
            Marshal.FreeHGlobal(preparsedData);

        _preparsedDataByDevice.Clear();
    }
}
