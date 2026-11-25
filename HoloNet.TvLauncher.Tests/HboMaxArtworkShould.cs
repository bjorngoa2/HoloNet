using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="HboMaxArtwork.Sized"/> only rewrites HBO Max CDN artwork URLs, and
/// replaces their requested width while keeping the other image options.
/// </summary>
public class HboMaxArtworkShould
{
    private const string Image = "https://beam-images.warnermediacdn.com/BEAM_LWM_DELIVERABLES/abc/def.jpg";

    [Fact]
    public void ReplaceTheRequestedWidth()
    {
        Assert.Equal($"{Image}?host=wbd-images.prod-vod.h264.io&w=640", HboMaxArtwork.Sized($"{Image}?host=wbd-images.prod-vod.h264.io&w=1920", 640));
    }

    [Fact]
    public void AddAWidth_WhenTheUrlHasNone()
    {
        Assert.Equal($"{Image}?w=640", HboMaxArtwork.Sized(Image, 640));
    }

    [Theory]
    [InlineData("https://example.com/image.jpg?w=1920")]
    [InlineData("not a url")]
    [InlineData(null)]
    public void LeaveOtherUrlsUnchanged(string? url)
    {
        Assert.Equal(url, HboMaxArtwork.Sized(url, 640));
    }
}
