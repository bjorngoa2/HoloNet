using System.Runtime.InteropServices;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Pins the exact bug this session found and fixed: <c>RawInputDeviceInfoHid</c> mirrors Win32's
/// <c>RID_DEVICE_INFO</c>, whose device-specific fields are a union of the mouse/keyboard/HID
/// variants. <c>GetRawInputDeviceInfo(RIDI_DEVICEINFO)</c> validates the caller's buffer against
/// the full union's size (the keyboard variant is the largest), regardless of which variant is
/// actually being queried. A struct sized only for the HID fields (24 bytes total) was 8 bytes
/// too small, making every call fail with ERROR_INSUFFICIENT_BUFFER (122) - which silently
/// resolved every device's vendor/product ID to 0x0000, which meant
/// <see cref="KnownGamepadReportFormats.Find"/> could never match, which reintroduced the
/// original "Bluetooth DualSense stops responding after quitting a game" bug via this session's
/// own refactor. See docs/tvlauncher-dualsense-bluetooth-fix.md for the full investigation.
///
/// If this test ever fails, it means the struct was shrunk back below the required 32 bytes -
/// exactly the mistake that caused the regression.
/// </summary>
public class RawInputDeviceInfoHidShould
{
    [Fact]
    public void BeSizedForTheFullRidDeviceInfoUnion()
    {
        // 8-byte RID_DEVICE_INFO header (cbSize + dwType) + 24-byte union (the RID_DEVICE_INFO_KEYBOARD
        // variant, the largest of the three) = 32 bytes total.
        const int requiredSize = 32;

        var actualSize = Marshal.SizeOf<RawInputGamepadReader.RawInputDeviceInfoHid>();

        Assert.Equal(requiredSize, actualSize);
    }
}
