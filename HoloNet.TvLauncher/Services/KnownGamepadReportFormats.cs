using System.Buffers.Binary;

namespace HoloNet.TvLauncher.Services;

/// <summary>
/// Standard CRC-32 (IEEE 802.3 / zlib) checksum, computed incrementally so a message can be
/// validated in parts (e.g. a fixed "seed" byte followed by the actual payload) without having
/// to allocate a single concatenated buffer first. Used by <see cref="KnownGamepadReportFormats"/>
/// to validate Sony's Bluetooth report CRCs before trusting a manually-parsed report.
/// </summary>
internal static class Crc32
{
    private const uint Polynomial = 0xEDB88320;
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var value = i;
            for (var bit = 0; bit < 8; bit++)
                value = (value & 1) != 0 ? Polynomial ^ (value >> 1) : value >> 1;
            table[i] = value;
        }

        return table;
    }

    /// <summary>Starting value for a new incremental CRC-32 computation.</summary>
    public const uint InitialValue = 0xFFFFFFFF;

    /// <summary>Folds <paramref name="data"/> into a running CRC-32 value (see <see cref="InitialValue"/> to start one).</summary>
    public static uint Append(uint runningCrc, ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            runningCrc = Table[(byte)(runningCrc ^ b)] ^ (runningCrc >> 8);

        return runningCrc;
    }

    /// <summary>Finalizes a running CRC-32 value into the value that would be transmitted/compared.</summary>
    public static uint Finalize(uint runningCrc) => ~runningCrc;
}

/// <summary>
/// Describes one specific "manufacturer-private" HID input report format that Windows' generic
/// HID parser can't read (see <see cref="RawInputGamepadReader"/> for why this is needed at
/// all), plus how to parse it. Matched by exact vendor/product/report-ID/length so this can
/// never misfire against some unrelated device that happens to share the same report shape.
/// </summary>
/// <param name="VendorId">USB vendor ID (e.g. <c>0x054C</c> for Sony).</param>
/// <param name="ProductIds">Known product IDs this format applies to (a manufacturer can reuse one report format across a whole controller family).</param>
/// <param name="ReportId">The HID report ID this format is for.</param>
/// <param name="Length">Exact report length (including the report ID byte) this format is for.</param>
/// <param name="Crc32Seed">
/// If set, the report is expected to end with a little-endian CRC-32 of every preceding byte
/// (including the report ID), seeded by feeding this single byte into the CRC first — reports
/// failing this check are corrupt/torn and must not be trusted. Null if the format has no CRC.
/// </param>
/// <param name="Parse">Fills <c>buttons</c> in place (indices matching <see cref="Configuration.TvLauncherOptions.DirectInputButtonMappings"/>) and returns the D-pad POV value.</param>
internal sealed record KnownExtendedReportFormat(
    ushort VendorId,
    IReadOnlySet<ushort> ProductIds,
    byte ReportId,
    int Length,
    byte? Crc32Seed,
    Func<byte[], bool[], int> Parse)
{
    public bool Matches(ushort vendorId, ushort productId, byte reportId, int length) =>
        vendorId == VendorId && ProductIds.Contains(productId) && reportId == ReportId && length == Length;

    /// <summary>Validates <see cref="Crc32Seed"/> against the report's trailing 4-byte little-endian CRC-32, if this format has one. Formats without a CRC always pass.</summary>
    public bool ValidateCrc32(byte[] report)
    {
        if (Crc32Seed is not { } seed)
            return true;

        var payloadLength = report.Length - sizeof(uint);
        if (payloadLength <= 0)
            return false;

        var crc = Crc32.InitialValue;
        crc = Crc32.Append(crc, [seed]);
        crc = Crc32.Append(crc, report.AsSpan(0, payloadLength));
        var computed = Crc32.Finalize(crc);

        var expected = BinaryPrimitives.ReadUInt32LittleEndian(report.AsSpan(payloadLength, sizeof(uint)));
        return computed == expected;
    }
}

/// <summary>
/// Known button-bit layout for Sony's DualSense controller, shared by both its USB and
/// Bluetooth "extended" reports (only the byte offset the buttons start at differs between the
/// two — see <see cref="KnownGamepadReportFormats"/>). Named bit masks mirror the
/// <c>DS_BUTTONS0_*</c>/<c>DS_BUTTONS1_*</c>/<c>DS_BUTTONS2_*</c> macros in Linux's
/// drivers/hid/hid-playstation.c, the reference this was verified against.
/// </summary>
internal static class DualSenseButtons
{
    private const byte HatSwitchMask = 0x0F;
    // HID POV units are hundredths of a degree (0-35900); the pad's 8-way hat reports 0-7,
    // so each step is 45 degrees = 4500 units. Same convention as the generic
    // HidP_GetUsageValue path this replaces.
    private const int PovUnitsPerHatStep = 4500;

    [Flags]
    private enum FaceButtonsAndHat : byte
    {
        Square = 0x10,
        Cross = 0x20,
        Circle = 0x40,
        Triangle = 0x80
    }

    [Flags]
    private enum ShoulderAndSystemButtons : byte
    {
        L1 = 0x01,
        R1 = 0x02,
        L2 = 0x04, // digital L2 press, separate from its analog value
        R2 = 0x08, // digital R2 press, separate from its analog value
        Create = 0x10, // "Create" (PS5) / "Share" (PS4)
        Options = 0x20,
        L3 = 0x40,
        R3 = 0x80
    }

    [Flags]
    private enum HomeAndTouchpadButtons : byte
    {
        PsHome = 0x01,
        TouchpadClick = 0x02
    }

    /// <summary>
    /// 0-based indices into the <c>buttons</c> array <see cref="Parse"/> fills, matching the
    /// HID Usage numbering the generic HidP_GetUsageValue path already produces (index = Usage
    /// ID - 1 — confirmed live: Usage ID 2 → Cross/Confirm at index 1, Usage ID 3 →
    /// Circle/Cancel at index 2, etc.), so this plugs into
    /// <see cref="Configuration.TvLauncherOptions.DirectInputButtonMappings"/> without any
    /// special-casing further up the stack.
    /// </summary>
    private enum ButtonIndex
    {
        Square = 0,
        Cross = 1,
        Circle = 2,
        Triangle = 3,
        L1 = 4,
        R1 = 5,
        L2 = 6,
        R2 = 7,
        Create = 8,
        Options = 9,
        L3 = 10,
        R3 = 11,
        PsHome = 12,
        TouchpadClick = 13
    }

    private static void SetButton(bool[] buttons, ButtonIndex index, bool value) =>
        buttons[(int)index] = value;

    /// <summary>
    /// Parses the three DualSense button bytes starting at <paramref name="buttonsOffset"/> in
    /// <paramref name="report"/>, filling <paramref name="buttons"/> in place (see
    /// <see cref="ButtonIndex"/> for the index convention). Returns the D-pad POV value (see
    /// <see cref="RawInputGamepadReader.Pov"/> for the value convention).
    /// </summary>
    public static int Parse(byte[] report, bool[] buttons, int buttonsOffset)
    {
        var faceButtonsAndHat = (FaceButtonsAndHat)report[buttonsOffset];
        var shoulderAndSystemButtons = (ShoulderAndSystemButtons)report[buttonsOffset + 1];
        var homeAndTouchpadButtons = (HomeAndTouchpadButtons)report[buttonsOffset + 2];

        SetButton(buttons, ButtonIndex.Square, faceButtonsAndHat.HasFlag(FaceButtonsAndHat.Square));
        SetButton(buttons, ButtonIndex.Cross, faceButtonsAndHat.HasFlag(FaceButtonsAndHat.Cross));
        SetButton(buttons, ButtonIndex.Circle, faceButtonsAndHat.HasFlag(FaceButtonsAndHat.Circle));
        SetButton(buttons, ButtonIndex.Triangle, faceButtonsAndHat.HasFlag(FaceButtonsAndHat.Triangle));
        SetButton(buttons, ButtonIndex.L1, shoulderAndSystemButtons.HasFlag(ShoulderAndSystemButtons.L1));
        SetButton(buttons, ButtonIndex.R1, shoulderAndSystemButtons.HasFlag(ShoulderAndSystemButtons.R1));
        SetButton(buttons, ButtonIndex.L2, shoulderAndSystemButtons.HasFlag(ShoulderAndSystemButtons.L2));
        SetButton(buttons, ButtonIndex.R2, shoulderAndSystemButtons.HasFlag(ShoulderAndSystemButtons.R2));
        SetButton(buttons, ButtonIndex.Create, shoulderAndSystemButtons.HasFlag(ShoulderAndSystemButtons.Create));
        SetButton(buttons, ButtonIndex.Options, shoulderAndSystemButtons.HasFlag(ShoulderAndSystemButtons.Options));
        SetButton(buttons, ButtonIndex.L3, shoulderAndSystemButtons.HasFlag(ShoulderAndSystemButtons.L3));
        SetButton(buttons, ButtonIndex.R3, shoulderAndSystemButtons.HasFlag(ShoulderAndSystemButtons.R3));
        SetButton(buttons, ButtonIndex.PsHome, homeAndTouchpadButtons.HasFlag(HomeAndTouchpadButtons.PsHome));
        SetButton(buttons, ButtonIndex.TouchpadClick, homeAndTouchpadButtons.HasFlag(HomeAndTouchpadButtons.TouchpadClick));

        // Standard HID hat switches report 0-7 for the eight directions and a "null state"
        // (commonly 8) when centered.
        var hat = report[buttonsOffset] & HatSwitchMask;
        return hat <= 7 ? hat * PovUnitsPerHatStep : -1;
    }
}

/// <summary>
/// Registry of HID input report formats that Windows' generic parser can't read but this app
/// knows how to parse manually (see <see cref="RawInputGamepadReader"/>). Add an entry here for
/// any other controller/report combination that turns up the same
/// <c>HIDP_STATUS_INCOMPATIBLE_REPORT_ID</c> gap in the future, rather than hand-rolling another
/// one-off parser.
/// </summary>
internal static class KnownGamepadReportFormats
{
    private const ushort SonyVendorId = 0x054C;

    // Both the standard DualSense and DualSense Edge report their Bluetooth "extended" input
    // report the same way; add further Sony pad product IDs here if they turn out to share it.
    private static readonly HashSet<ushort> DualSenseProductIds = [0x0CE6, 0x0DF2];

    // Sony's own Linux driver (hid-playstation.c) rejects this report if its CRC doesn't
    // validate, so this fallback does the same rather than trusting a possibly-torn report.
    private const byte DualSenseInputCrc32Seed = 0xA1;

    public static readonly IReadOnlyList<KnownExtendedReportFormat> All =
    [
        new KnownExtendedReportFormat(
            VendorId: SonyVendorId,
            ProductIds: DualSenseProductIds,
            ReportId: 0x31,
            Length: 78,
            Crc32Seed: DualSenseInputCrc32Seed,
            // The report's first 2 bytes are the report ID and a Bluetooth sequence/tag byte,
            // after which the common DualSense report layout begins — button bytes land at
            // offset 9 of the full report (offset 7 within that common layout).
            Parse: (report, buttons) => DualSenseButtons.Parse(report, buttons, buttonsOffset: 9))
    ];

    public static KnownExtendedReportFormat? Find(ushort vendorId, ushort productId, byte reportId, int length) =>
        All.FirstOrDefault(format => format.Matches(vendorId, productId, reportId, length));
}
