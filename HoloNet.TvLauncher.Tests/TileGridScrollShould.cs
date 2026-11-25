using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="TileGridScroll.OffsetFor"/> keeps a short grid still and pins the focused
/// row of a long one to the top - including near the end of the list, where clamping to the
/// scrollable height used to leave a sliver of the row above showing.
/// </summary>
public class TileGridScrollShould
{
    private const double ViewportHeight = 500;

    [Fact]
    public void NotScroll_WhenEveryTileIsClearOfTheBottomFade()
    {
        var contentBottom = ViewportHeight - TileGridScroll.BottomFadeHeight;

        Assert.Equal(0, TileGridScroll.OffsetFor(rowTop: 212, contentBottom, ViewportHeight));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(212)]
    [InlineData(636)]
    public void PinTheFocusedRowToTheTop_WhenTheGridDoesNotFit(double rowTop)
    {
        Assert.Equal(rowTop, TileGridScroll.OffsetFor(rowTop, contentBottom: 878, ViewportHeight));
    }

    [Fact]
    public void Scroll_WhenTheLastRowWouldOnlyShowFaded()
    {
        var contentBottom = ViewportHeight - TileGridScroll.BottomFadeHeight + 1;

        Assert.Equal(212, TileGridScroll.OffsetFor(rowTop: 212, contentBottom, ViewportHeight));
    }
}
