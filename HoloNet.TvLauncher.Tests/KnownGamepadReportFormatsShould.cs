using System.Buffers.Binary;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Covers the fallback-report registry (<see cref="KnownGamepadReportFormats"/>) that fixed the
/// "Bluetooth DualSense stops responding after quitting a game" bug — see
/// docs/tvlauncher-dualsense-bluetooth-fix.md. The regression this session found and fixed was
/// that <see cref="KnownGamepadReportFormats.Find"/> silently never matched anything once the
/// vendor/product ID lookup started failing (always resolving to 0x0000/0x0000), so the most
/// important case here is proving that specific "unresolved IDs" scenario returns null instead
/// of throwing or silently matching the wrong device.
/// </summary>
public class KnownGamepadReportFormatsShould
{
    private const ushort SonyVendorId = 0x054C;
    private const ushort DualSenseProductId = 0x0CE6;
    private const ushort DualSenseEdgeProductId = 0x0DF2;
    private const byte DualSenseReportId = 0x31;
    private const int DualSenseReportLength = 78;

    /// <summary>
    /// Builds a syntactically valid 78-byte DualSense Bluetooth extended report with the given
    /// button bytes and a correct trailing CRC-32, matching the exact layout
    /// <see cref="KnownGamepadReportFormats"/> expects.
    /// </summary>
    private static byte[] BuildValidReport(byte buttons0 = 0, byte buttons1 = 0, byte buttons2 = 0)
    {
        var report = new byte[DualSenseReportLength];
        report[0] = DualSenseReportId;
        report[9] = buttons0;
        report[10] = buttons1;
        report[11] = buttons2;

        var payloadLength = report.Length - sizeof(uint);
        var crc = Crc32.InitialValue;
        crc = Crc32.Append(crc, [0xA1]); // DualSenseInputCrc32Seed
        crc = Crc32.Append(crc, report.AsSpan(0, payloadLength));
        var checksum = Crc32.Finalize(crc);

        BinaryPrimitives.WriteUInt32LittleEndian(report.AsSpan(payloadLength, sizeof(uint)), checksum);
        return report;
    }

    [Theory]
    [InlineData(DualSenseProductId)]
    [InlineData(DualSenseEdgeProductId)]
    public void Find_ReturnDualSenseFormat_ForKnownSonyVendorAndProductId(ushort productId)
    {
        var format = KnownGamepadReportFormats.Find(SonyVendorId, productId, DualSenseReportId, DualSenseReportLength);

        Assert.NotNull(format);
    }

    [Fact]
    public void Find_ReturnNull_WhenVendorAndProductIdsAreUnresolved()
    {
        // This is the exact regression this session found: GetRawInputDeviceInfo failing
        // (ERROR_INSUFFICIENT_BUFFER) caused vendor/product ID to always resolve to 0x0000, and
        // the fallback lookup must fail closed in that case rather than matching a device it
        // was never verified against.
        var format = KnownGamepadReportFormats.Find(0x0000, 0x0000, DualSenseReportId, DualSenseReportLength);

        Assert.Null(format);
    }

    [Fact]
    public void Find_ReturnNull_ForUnrelatedVendorId()
    {
        var format = KnownGamepadReportFormats.Find(0x045E, DualSenseProductId, DualSenseReportId, DualSenseReportLength);

        Assert.Null(format);
    }

    [Theory]
    [InlineData((byte)0x01, DualSenseReportLength)] // wrong report ID
    [InlineData(DualSenseReportId, 64)] // wrong length
    public void Find_ReturnNull_ForWrongReportIdOrLength(byte reportId, int length)
    {
        var format = KnownGamepadReportFormats.Find(SonyVendorId, DualSenseProductId, reportId, length);

        Assert.Null(format);
    }

    [Fact]
    public void ValidateCrc32_ReturnTrue_ForWellFormedReport()
    {
        var format = KnownGamepadReportFormats.Find(SonyVendorId, DualSenseProductId, DualSenseReportId, DualSenseReportLength);
        var report = BuildValidReport();

        Assert.True(format!.ValidateCrc32(report));
    }

    [Fact]
    public void ValidateCrc32_ReturnFalse_ForCorruptedReport()
    {
        var format = KnownGamepadReportFormats.Find(SonyVendorId, DualSenseProductId, DualSenseReportId, DualSenseReportLength);
        var report = BuildValidReport();
        report[9] ^= 0xFF; // flip a button byte after the CRC was computed - simulates a torn/corrupt report

        Assert.False(format!.ValidateCrc32(report));
    }

    [Fact]
    public void Parse_DecodeButtonsAndPov_ThroughTheRegisteredFormat()
    {
        var format = KnownGamepadReportFormats.Find(SonyVendorId, DualSenseProductId, DualSenseReportId, DualSenseReportLength);
        // Cross (0x20) pressed, hat centered (0x08).
        var report = BuildValidReport(buttons0: 0x28);
        var buttons = new bool[14];

        var pov = format!.Parse(report, buttons);

        Assert.True(buttons[1]); // Cross / Confirm
        Assert.False(buttons[0]); // Square not pressed
        Assert.Equal(-1, pov); // centered
    }
}
