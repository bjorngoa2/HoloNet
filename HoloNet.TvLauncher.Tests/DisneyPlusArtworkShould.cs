using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="DisneyPlusArtwork.Sized"/> only rewrites Disney+ CDN artwork URLs, and
/// replaces their requested width while keeping the other image options.
/// </summary>
public class DisneyPlusArtworkShould
{
    private const string Original =
        "https://disney.images.edge.bamgrid.com/ripcut-delivery/v2/variant/disney/abc-123/compose?format=jpeg&width=1920";

    [Fact]
    public void ReplaceTheRequestedWidth()
    {
        Assert.Equal(
            "https://disney.images.edge.bamgrid.com/ripcut-delivery/v2/variant/disney/abc-123/compose?format=jpeg&width=640",
            DisneyPlusArtwork.Sized(Original, 640));
    }

    [Fact]
    public void AddAWidth_WhenTheUrlHasNone()
    {
        Assert.Equal(
            "https://disney.images.edge.bamgrid.com/ripcut-delivery/v2/variant/disney/abc-123/compose?format=jpeg&width=640",
            DisneyPlusArtwork.Sized("https://disney.images.edge.bamgrid.com/ripcut-delivery/v2/variant/disney/abc-123/compose?format=jpeg", 640));
    }

    [Theory]
    [InlineData("https://example.com/compose?width=1920")]
    [InlineData("not a url")]
    [InlineData(null)]
    public void LeaveOtherUrlsUnchanged(string? url)
    {
        Assert.Equal(url, DisneyPlusArtwork.Sized(url, 640));
    }
}
