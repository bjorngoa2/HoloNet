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
/// align with DirectInput's 0-based button array indices) — with opt-in debug logging (see
/// <see cref="GamepadDebugLog"/>) so that assumption can actually be verified against live
/// hardware instead of shipping unverified again.
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

    // DualSense Bluetooth "extended" input report — see class doc comment above the fallback
    // parser below. Values confirmed against Linux's drivers/hid/hid-playstation.c.
    private const byte DualSenseBtExtendedReportId = 0x31;
    private const int DualSenseBtExtendedReportSize = 78;
    private const int DualSenseBtButtons0Offset = 9;
    private const int DualSenseBtButtons1Offset = 10;
    private const int DualSenseBtButtons2Offset = 11;

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
    private byte _lastReportId = 0xFF;
    private int _lastReportLength = -1;
    private int _lastButtonStatus = int.MinValue;
    private int _lastPovStatus = int.MinValue;

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
        GamepadDebugLog.Log($"Attach windowHandle=0x{windowHandle:X} registered={registered} lastError={(registered ? 0 : Marshal.GetLastWin32Error())}");
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
            GamepadDebugLog.Log($"ProcessRawInput threw: {ex}");
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

            // Confirmed via live testing: as soon as something (SDL/PCSX2) touches a Bluetooth
            // DualSense, it switches from its "basic" report (ID 1, which the generic HidP_*
            // calls above parse fine) to its "extended" report (ID 0x31/49, 78 bytes) — and it
            // stays there even after that app closes, until the pad is power-cycled. Windows'
            // preparsed HID descriptor has no usage collection for report ID 0x31 at all, so
            // HidP_GetUsages/GetUsageValue return HIDP_STATUS_INCOMPATIBLE_REPORT_ID (0xC011000A)
            // for it — this is a structural gap, not a bug in the calls above. Parse that report
            // manually instead, using the fixed byte layout Sony's own Linux driver
            // (drivers/hid/hid-playstation.c, dualsense_parse_report) uses for it, so the pad
            // keeps working without ever needing a manual power-cycle.
            if (buttonStatus != HidPStatusSuccess && reportLength == DualSenseBtExtendedReportSize
                && report[0] == DualSenseBtExtendedReportId)
            {
                ParseDualSenseBluetoothExtendedReport(report, buttons, out pov);
            }

            Buttons = buttons;
            Pov = pov;
            HasReceivedData = true;

            // Diagnostic-only: log whenever the report's identity (ID/length) or whether we can
            // successfully parse it changes at all, independent of whether the *parsed* button/
            // pov values changed. This is deliberately NOT deduped against button/pov state,
            // because a device that has switched HID report formats (e.g. a Bluetooth DualSense
            // entering its "extended" mode) can keep sending WM_INPUT messages that consistently
            // fail to parse (buttonStatus/povStatus != success) while the last successfully
            // parsed Buttons/Pov stay frozen — LogIfChanged alone would go completely silent in
            // that case even though data is still arriving, hiding the real failure mode.
            var reportId = report.Length > 0 ? report[0] : (byte)0xFF;
            if (reportId != _lastReportId || reportLength != _lastReportLength
                || buttonStatus != _lastButtonStatus || povStatus != _lastPovStatus)
            {
                _lastReportId = reportId;
                _lastReportLength = reportLength;
                _lastButtonStatus = buttonStatus;
                _lastPovStatus = povStatus;
                GamepadDebugLog.Log($"report reportId={reportId} length={reportLength} buttonStatus=0x{buttonStatus:X} povStatus=0x{povStatus:X}");
            }

            LogIfChanged(buttons, pov);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Manually parses a DualSense Bluetooth "extended" input report (ID 0x31, 78 bytes),
    /// which Windows' generic HID parser cannot handle (see the call site above). Byte offsets
    /// and bit masks below match <c>struct dualsense_input_report</c> and the
    /// <c>DS_BUTTONS0_*</c>/<c>DS_BUTTONS1_*</c>/<c>DS_BUTTONS2_*</c> masks in Linux's
    /// drivers/hid/hid-playstation.c: the report's first 2 bytes are the report ID and a
    /// Bluetooth sequence/tag byte, after which the common DualSense report layout begins —
    /// button bytes land at offsets 9-11 of the full report. Button indices are assigned to
    /// match the same 0-based ordering the generic HID Usage path already produces (confirmed
    /// live: Usage ID 2 → Cross/Confirm at index 1, Usage ID 3 → Circle/Cancel at index 2, etc.),
    /// so this fallback plugs into <see cref="Configuration.TvLauncherOptions.DirectInputButtonMappings"/>
    /// without any special-casing further up the stack.
    /// </summary>
    private static void ParseDualSenseBluetoothExtendedReport(byte[] report, bool[] buttons, out int pov)
    {
        var buttons0 = report[DualSenseBtButtons0Offset];
        var buttons1 = report[DualSenseBtButtons1Offset];
        var buttons2 = report[DualSenseBtButtons2Offset];

        buttons[0] = (buttons0 & 0x10) != 0; // Square
        buttons[1] = (buttons0 & 0x20) != 0; // Cross
        buttons[2] = (buttons0 & 0x40) != 0; // Circle
        buttons[3] = (buttons0 & 0x80) != 0; // Triangle
        buttons[4] = (buttons1 & 0x01) != 0; // L1
        buttons[5] = (buttons1 & 0x02) != 0; // R1
        buttons[6] = (buttons1 & 0x04) != 0; // L2 (digital)
        buttons[7] = (buttons1 & 0x08) != 0; // R2 (digital)
        buttons[8] = (buttons1 & 0x10) != 0; // Create/Share
        buttons[9] = (buttons1 & 0x20) != 0; // Options
        buttons[10] = (buttons1 & 0x40) != 0; // L3
        buttons[11] = (buttons1 & 0x80) != 0; // R3
        buttons[12] = (buttons2 & 0x01) != 0; // PS/Home
        buttons[13] = (buttons2 & 0x02) != 0; // Touchpad click

        // Low nibble of buttons0 is the D-pad hat switch: 0-7 for the eight directions, 8 for
        // centered — same convention as the generic HidP_GetUsageValue path this replaces.
        var hat = buttons0 & 0x0F;
        pov = hat <= 7 ? hat * 4500 : -1;
    }

    private void LogIfChanged(bool[] buttons, int pov)
    {
        if (pov == _lastLoggedPov && buttons.AsSpan().SequenceEqual(_lastLoggedButtons))
            return;

        _lastLoggedPov = pov;
        _lastLoggedButtons = (bool[])buttons.Clone();

        var pressedIndices = string.Join(",", Enumerable.Range(0, buttons.Length).Where(i => buttons[i]));
        GamepadDebugLog.Log($"state pov={pov} pressed=[{pressedIndices}]");
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
        GamepadDebugLog.Log($"Registered preparsed data for device=0x{device:X}");
        return buffer;
    }

    public void Dispose()
    {
        foreach (var preparsedData in _preparsedDataByDevice.Values)
            Marshal.FreeHGlobal(preparsedData);

        _preparsedDataByDevice.Clear();
    }
}
