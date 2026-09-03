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
///
/// When the OS's generic HID parser can't read a device's current report at all (observed with
/// the DualSense's Bluetooth "extended" report, entered once anything communicates with the pad
/// over SDL/hidapi, and never left until it's power-cycled), this falls back to a small registry
/// of manually-parsed, exact vendor/product/report-ID/length-matched formats — see
/// <see cref="KnownGamepadReportFormats"/>.
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
    private const uint RidiDeviceInfo = 0x2000000B;

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

    /// <summary>
    /// The HID-specific arm of the <c>RID_DEVICE_INFO</c> union (Win32's <c>RID_DEVICE_INFO_HID</c>).
    /// Only valid when queried for a device whose type is <see cref="RimTypeHid"/> — used solely
    /// to identify vendor/product ID so a manually-parsed report format (see
    /// <see cref="KnownGamepadReportFormats"/>) is never applied to a device it wasn't verified
    /// against.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDeviceInfoHid
    {
        public uint Size;
        public uint Type;
        public uint VendorId;
        public uint ProductId;
        public uint VersionNumber;
        public ushort UsagePage;
        public ushort Usage;

        // RID_DEVICE_INFO's device-specific fields are a union with RID_DEVICE_INFO_MOUSE and
        // RID_DEVICE_INFO_KEYBOARD, not just this HID variant. RID_DEVICE_INFO_KEYBOARD is the
        // largest of the three (6 DWORDs vs. this struct's 3 DWORDs + 2 WORDs), and
        // GetRawInputDeviceInfo validates the caller's buffer against that full union size
        // regardless of which variant the device actually is — passing a buffer sized only for
        // the HID fields fails with ERROR_INSUFFICIENT_BUFFER (122) even though those extra
        // bytes are never populated for a HID device. This padding exists purely so the buffer
        // size matches; its value is never read.
        private readonly uint _unusedKeyboardUnionPadding1;
        private readonly uint _unusedKeyboardUnionPadding2;
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
    private readonly Dictionary<IntPtr, (ushort VendorId, ushort ProductId)> _deviceIdsByDevice = new();

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
            // calls above parse fine) to a manufacturer-private "extended" report — and it stays
            // there even after that app closes, until the pad is power-cycled. Windows'
            // preparsed HID descriptor has no usage collection for that report ID at all, so
            // HidP_GetUsages/GetUsageValue return HIDP_STATUS_INCOMPATIBLE_REPORT_ID (0xC011000A)
            // for it — this is a structural gap, not a bug in the calls above. Fall back to a
            // manually-parsed, exact-match report format for this specific device (see
            // KnownGamepadReportFormats) instead, so the pad keeps working without ever needing
            // a manual power-cycle.
            string? fallbackOutcome = null;
            if (buttonStatus != HidPStatusSuccess)
            {
                var (vendorId, productId) = GetOrFetchDeviceIds(header.Device);
                var format = KnownGamepadReportFormats.Find(vendorId, productId, report[0], reportLength);
                if (format is null)
                {
                    // No known manual fallback for this device/report combination — nothing more
                    // we can do; leave buttons/pov at their all-centered defaults.
                    fallbackOutcome = $"no known fallback format for vendorId=0x{vendorId:X4} productId=0x{productId:X4}";
                }
                else if (!format.ValidateCrc32(report))
                {
                    // A corrupt/torn report must never be trusted — leaving buttons/pov centered
                    // is far safer than acting on garbage input.
                    fallbackOutcome = "fallback format matched but CRC-32 validation failed";
                }
                else
                {
                    pov = format.Parse(report, buttons);
                    fallbackOutcome = "fallback format matched and parsed";
                }
            }

            Buttons = buttons;
            Pov = pov;
            HasReceivedData = true;

            // Diagnostic-only: log whenever the report's identity (ID/length), whether we can
            // successfully parse it, or the fallback outcome changes at all, independent of
            // whether the *parsed* button/pov values changed. This is deliberately NOT deduped
            // against button/pov state, because a device that has switched HID report formats
            // (e.g. a Bluetooth DualSense entering its "extended" mode) can keep sending
            // WM_INPUT messages that consistently fail to parse (buttonStatus/povStatus !=
            // success) while the last successfully parsed Buttons/Pov stay frozen — LogIfChanged
            // alone would go completely silent in that case even though data is still arriving,
            // hiding the real failure mode.
            var reportId = report.Length > 0 ? report[0] : (byte)0xFF;
            if (reportId != _lastReportId || reportLength != _lastReportLength
                || buttonStatus != _lastButtonStatus || povStatus != _lastPovStatus)
            {
                _lastReportId = reportId;
                _lastReportLength = reportLength;
                _lastButtonStatus = buttonStatus;
                _lastPovStatus = povStatus;
                var suffix = fallbackOutcome is null ? string.Empty : $" fallback=[{fallbackOutcome}]";
                GamepadDebugLog.Log($"report reportId={reportId} length={reportLength} buttonStatus=0x{buttonStatus:X} povStatus=0x{povStatus:X}{suffix}");
            }

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

    /// <summary>Fetches (and caches) a device's USB vendor/product ID, used to gate manually-parsed report formats to the exact hardware they were verified against (see <see cref="KnownGamepadReportFormats"/>).</summary>
    private (ushort VendorId, ushort ProductId) GetOrFetchDeviceIds(IntPtr device)
    {
        if (_deviceIdsByDevice.TryGetValue(device, out var cached))
            return cached;

        var result = (VendorId: (ushort)0, ProductId: (ushort)0);
        var size = (uint)Marshal.SizeOf<RawInputDeviceInfoHid>();
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            Marshal.WriteInt32(buffer, (int)size); // RID_DEVICE_INFO.cbSize must be pre-populated.

            if (GetRawInputDeviceInfo(device, RidiDeviceInfo, buffer, ref size) != unchecked((uint)-1))
            {
                var info = Marshal.PtrToStructure<RawInputDeviceInfoHid>(buffer);
                if (info.Type == RimTypeHid)
                    result = ((ushort)info.VendorId, (ushort)info.ProductId);
            }
            else
            {
                GamepadDebugLog.Log($"GetRawInputDeviceInfo(RIDI_DEVICEINFO) failed for device=0x{device:X} lastError={Marshal.GetLastWin32Error()}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        _deviceIdsByDevice[device] = result;
        GamepadDebugLog.Log($"Device 0x{device:X} vendorId=0x{result.VendorId:X4} productId=0x{result.ProductId:X4}");
        return result;
    }

    public void Dispose()
    {
        foreach (var preparsedData in _preparsedDataByDevice.Values)
            Marshal.FreeHGlobal(preparsedData);

        _preparsedDataByDevice.Clear();
        _deviceIdsByDevice.Clear();
    }
}
