using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="PrimeVideoArtwork.Sized"/> only rewrites Amazon CDN artwork URLs, and
/// inserts the size modifier where the CDN expects it.
/// </summary>
public class PrimeVideoArtworkShould
{
    private const string Original = "https://m.media-amazon.com/images/S/pv-target-images/abc123.png";

    [Fact]
    public void InsertTheSizeModifier_BeforeTheExtension()
    {
        Assert.Equal(
            "https://m.media-amazon.com/images/S/pv-target-images/abc123._SX640_.png",
            PrimeVideoArtwork.Sized(Original, 640));
    }

    [Theory]
    [InlineData("https://m.media-amazon.com/images/S/pv-target-images/abc123._SX1920_.png")]
    [InlineData("https://example.com/images/poster.png")]
    [InlineData("https://m.media-amazon.com/other/poster.png")]
    [InlineData("not a url")]
    [InlineData(null)]
    public void LeaveOtherUrlsUnchanged(string? url)
    {
        Assert.Equal(url, PrimeVideoArtwork.Sized(url, 640));
    }
}
