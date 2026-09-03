using System.Runtime.InteropServices;

namespace HoloNet.TvLauncher.Services;

/// <summary>
/// Detects the "quit current game" button combo (Share+Options on a PS4/PS5 pad, Back+Start on
/// an Xbox pad) using the Windows <c>Raw Input</c> API (<c>RegisterRawInputDevices</c>/<c>WM_INPUT</c>)
/// instead of DirectInput.
///
/// This exists purely as a fallback for Bluetooth-connected DualShock/DualSense pads: PCSX2's
/// input backend can leave <see cref="GamepadInputService"/>'s DirectInput polling unable to read
/// ANY input from a Bluetooth pad while a game is running — see the README's "PS4/PS5 controller
/// over Bluetooth + PCSX2" section — apparently a DirectInput/legacy-joystick report-format
/// compatibility gap with the DualSense's Bluetooth report mode, not simple device exclusivity
/// (this persisted even after switching PCSX2's own input source away from SDL). Raw Input reads
/// HID reports through a different Windows subsystem — the same one SDL itself falls back to
/// internally (<c>SDL_rawinputjoystick.c</c>) for exactly this kind of multi-consumer scenario —
/// so it can keep working even when DirectInput goes blind.
///
/// Deliberately narrow in scope: this ONLY detects the quit combo, not full menu navigation, to
/// minimize new surface area in an app whose existing XInput/DirectInput navigation already
/// works well. It runs *alongside*, not instead of, the existing polling — wired/USB controllers
/// are completely unaffected, since this is purely an additional signal source feeding the same
/// "Quit" event that <see cref="GamepadInputService"/> already raises.
///
/// Button identification reuses <see cref="Configuration.TvLauncherOptions.DirectInputButtonMappings"/>
/// (the "Share"/"Refresh" entries) rather than introducing a second mapping to configure: Raw
/// Input's HID button Usage IDs are 1-based and align with DirectInput's 0-based button array
/// indices (Usage ID = index + 1) for standard HID game controllers, so the same configured
/// indices apply to both paths.
/// </summary>
internal sealed class RawInputQuitComboListener : IDisposable
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
    private const int HidPInput = 0;
    private const int HidPStatusSuccess = 0x00110000;
    private const int MaxTrackedButtons = 64;

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

    private readonly Func<IReadOnlyDictionary<string, List<int>>> _getButtonMappings;
    private readonly Func<int> _getQuitHoldMilliseconds;
    private readonly Action _raiseQuit;
    private readonly ComboHoldTracker _combo = new();
    private readonly Dictionary<IntPtr, IntPtr> _preparsedDataByDevice = new();

    public RawInputQuitComboListener(
        Func<IReadOnlyDictionary<string, List<int>>> getButtonMappings,
        Func<int> getQuitHoldMilliseconds,
        Action raiseQuit)
    {
        _getButtonMappings = getButtonMappings;
        _getQuitHoldMilliseconds = getQuitHoldMilliseconds;
        _raiseQuit = raiseQuit;
    }

    /// <summary>
    /// Registers for gamepad/joystick Raw Input on the given window, with <c>RIDEV_INPUTSINK</c>
    /// so reports keep arriving even while another application (the emulator) has focus — the
    /// same behavior DirectInput's background/non-exclusive acquisition already relies on.
    /// </summary>
    public void Attach(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return;

        var devices = new[]
        {
            new RawInputDevice { UsagePage = HidUsagePageGeneric, Usage = HidUsageGamepad, Flags = RidevInputSink, Target = windowHandle },
            new RawInputDevice { UsagePage = HidUsagePageGeneric, Usage = HidUsageJoystick, Flags = RidevInputSink, Target = windowHandle }
        };

        RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>());
    }

    /// <summary>Call from the host window's message hook for every message; only <c>WM_INPUT</c> is acted on.</summary>
    public void HandleWindowMessage(int msg, IntPtr lParam)
    {
        if (msg != WmInput)
            return;

        try
        {
            ProcessRawInput(lParam);
        }
        catch (Exception)
        {
            // Defensive: a malformed/unexpected raw input payload must never crash the picker's
            // message loop — this is a best-effort fallback signal, not a critical path.
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
            var status = HidP_GetUsages(
                HidPInput, HidUsagePageButton, 0, usageList, ref usageLength, preparsedData, report, (uint)report.Length);

            if (status != HidPStatusSuccess)
                return;

            var pressed = new HashSet<int>();
            for (var i = 0; i < usageLength; i++)
                pressed.Add(usageList[i] - 1); // HID usage IDs are 1-based; convert to a 0-based index.

            var mappings = _getButtonMappings();
            var comboPressed = AnyPressed(pressed, mappings, "Share") && AnyPressed(pressed, mappings, "Refresh");

            if (_combo.Evaluate(comboPressed, _getQuitHoldMilliseconds()))
                _raiseQuit();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool AnyPressed(HashSet<int> pressedIndices, IReadOnlyDictionary<string, List<int>> mappings, string key)
    {
        if (!mappings.TryGetValue(key, out var indices))
            return false;

        foreach (var index in indices)
        {
            if (pressedIndices.Contains(index))
                return true;
        }

        return false;
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
        return buffer;
    }

    public void Dispose()
    {
        foreach (var preparsedData in _preparsedDataByDevice.Values)
            Marshal.FreeHGlobal(preparsedData);

        _preparsedDataByDevice.Clear();
    }
}
