using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies the reusable CRC-32 implementation used by <see cref="KnownGamepadReportFormats"/>
/// to reject torn/corrupt DualSense Bluetooth reports before trusting them.
/// </summary>
public class Crc32Should
{
    [Fact]
    public void Append_MatchStandardCrc32IeeeTestVector()
    {
        // "123456789" is the canonical CRC-32 (IEEE 802.3) test vector; the expected value
        // (0xCBF43926) is published alongside the algorithm and independent of this codebase,
        // so this proves the implementation itself is correct rather than self-consistent.
        var bytes = "123456789"u8.ToArray();

        var crc = Crc32.InitialValue;
        crc = Crc32.Append(crc, bytes);
        var result = Crc32.Finalize(crc);

        Assert.Equal(0xCBF43926u, result);
    }

    [Fact]
    public void Append_ProduceSameResult_WhenDataIsSplitAcrossMultipleCalls()
    {
        var full = "the quick brown fox"u8.ToArray();

        var oneShot = Crc32.Finalize(Crc32.Append(Crc32.InitialValue, full));

        var running = Crc32.InitialValue;
        running = Crc32.Append(running, full.AsSpan(0, 7));
        running = Crc32.Append(running, full.AsSpan(7));
        var splitResult = Crc32.Finalize(running);

        Assert.Equal(oneShot, splitResult);
    }
}
