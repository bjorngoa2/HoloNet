using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Covers <see cref="DualSenseButtons.Parse"/> directly - the byte-level decoding that replaced
/// the old hardcoded parser (see docs/tvlauncher-dualsense-bluetooth-fix.md). Each button/hat
/// case is verified in isolation so a future refactor of the bit masks or button-index mapping
/// can't silently swap two buttons without a test failing.
/// </summary>
public class DualSenseButtonsShould
{
    private const int ButtonsOffset = 9;

    private static byte[] MakeReport(byte buttons0 = 0, byte buttons1 = 0, byte buttons2 = 0)
    {
        var report = new byte[ButtonsOffset + 3];
        report[ButtonsOffset] = buttons0;
        report[ButtonsOffset + 1] = buttons1;
        report[ButtonsOffset + 2] = buttons2;
        return report;
    }

    [Theory]
    [InlineData(0x10, 0)] // Square
    [InlineData(0x20, 1)] // Cross
    [InlineData(0x40, 2)] // Circle
    [InlineData(0x80, 3)] // Triangle
    public void Parse_DecodeFaceButtons_AtCorrectIndex(byte buttons0Bit, int expectedIndex)
    {
        var report = MakeReport(buttons0: (byte)(buttons0Bit | 0x08)); // hat centered
        var buttons = new bool[14];

        DualSenseButtons.Parse(report, buttons, ButtonsOffset);

        for (var i = 0; i < buttons.Length; i++)
            Assert.Equal(i == expectedIndex, buttons[i]);
    }

    [Theory]
    [InlineData(0x01, 4)] // L1
    [InlineData(0x02, 5)] // R1
    [InlineData(0x04, 6)] // L2 (digital press)
    [InlineData(0x08, 7)] // R2 (digital press)
    [InlineData(0x10, 8)] // Create/Share
    [InlineData(0x20, 9)] // Options
    [InlineData(0x40, 10)] // L3
    [InlineData(0x80, 11)] // R3
    public void Parse_DecodeShoulderAndSystemButtons_AtCorrectIndex(byte buttons1Bit, int expectedIndex)
    {
        var report = MakeReport(buttons0: 0x08, buttons1: buttons1Bit); // hat centered
        var buttons = new bool[14];

        DualSenseButtons.Parse(report, buttons, ButtonsOffset);

        for (var i = 0; i < buttons.Length; i++)
            Assert.Equal(i == expectedIndex, buttons[i]);
    }

    [Theory]
    [InlineData(0x01, 12)] // PS/Home
    [InlineData(0x02, 13)] // Touchpad click
    public void Parse_DecodeHomeAndTouchpadButtons_AtCorrectIndex(byte buttons2Bit, int expectedIndex)
    {
        var report = MakeReport(buttons0: 0x08, buttons2: buttons2Bit); // hat centered
        var buttons = new bool[14];

        DualSenseButtons.Parse(report, buttons, ButtonsOffset);

        for (var i = 0; i < buttons.Length; i++)
            Assert.Equal(i == expectedIndex, buttons[i]);
    }

    [Theory]
    [InlineData(0x00, 0)] // Up
    [InlineData(0x01, 4500)] // Up-right
    [InlineData(0x02, 9000)] // Right
    [InlineData(0x03, 13500)] // Down-right
    [InlineData(0x04, 18000)] // Down
    [InlineData(0x05, 22500)] // Down-left
    [InlineData(0x06, 27000)] // Left
    [InlineData(0x07, 31500)] // Up-left
    [InlineData(0x08, -1)] // Centered (the "null state")
    [InlineData(0x0F, -1)] // Out-of-range value also treated as centered
    public void Parse_DecodeHatSwitch_ToExpectedPovValue(byte hatValue, int expectedPov)
    {
        var report = MakeReport(buttons0: hatValue);
        var buttons = new bool[14];

        var pov = DualSenseButtons.Parse(report, buttons, ButtonsOffset);

        Assert.Equal(expectedPov, pov);
    }

    [Fact]
    public void Parse_DecodeMultipleSimultaneousButtons()
    {
        // Cross + Circle + L1 + Options all held at once.
        var report = MakeReport(buttons0: (byte)(0x20 | 0x40 | 0x08), buttons1: (byte)(0x01 | 0x20));
        var buttons = new bool[14];

        DualSenseButtons.Parse(report, buttons, ButtonsOffset);

        Assert.True(buttons[1]); // Cross
        Assert.True(buttons[2]); // Circle
        Assert.True(buttons[4]); // L1
        Assert.True(buttons[9]); // Options
        Assert.False(buttons[0]); // Square
        Assert.False(buttons[5]); // R1
    }
}
