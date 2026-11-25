using System.Windows;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="SpatialNavigator"/> picks the visually adjacent tile from on-screen
/// geometry - including the layouts index arithmetic gets wrong (a short last row, and mixed
/// tile widths where rows don't share columns).
/// </summary>
public class SpatialNavigatorShould
{
    private const double Width = 100;
    private const double Height = 60;
    private const double Spacing = 20;

    /// <summary>A uniform grid laid out left-to-right, wrapping after <paramref name="columns"/>.</summary>
    private static List<Rect> Grid(int count, int columns) =>
        Enumerable.Range(0, count)
            .Select(i => new Rect((i % columns) * (Width + Spacing), (i / columns) * (Height + Spacing), Width, Height))
            .ToList();

    [Theory]
    [InlineData(0, NavigationDirection.Right, 1)]
    [InlineData(1, NavigationDirection.Left, 0)]
    [InlineData(1, NavigationDirection.Down, 4)]
    [InlineData(5, NavigationDirection.Up, 2)]
    public void MoveToTheAdjacentTile_InAGrid(int from, NavigationDirection direction, int expected)
    {
        Assert.Equal(expected, SpatialNavigator.FindNext(Grid(count: 6, columns: 3), from, direction));
    }

    [Theory]
    [InlineData(2, NavigationDirection.Right)]
    [InlineData(3, NavigationDirection.Left)]
    [InlineData(0, NavigationDirection.Up)]
    public void StayPut_AtAnEdge(int from, NavigationDirection direction)
    {
        Assert.Null(SpatialNavigator.FindNext(Grid(count: 6, columns: 3), from, direction));
    }

    [Fact]
    public void LandOnTheNearestTileOfAShortLastRow_WhenMovingDown()
    {
        // Row 1 has tiles 0-3, row 2 only has tiles 4-5: down from tile 3 should land on the
        // closest tile below (5), not be ignored because "index + columns" doesn't exist.
        Assert.Equal(5, SpatialNavigator.FindNext(Grid(count: 6, columns: 4), 3, NavigationDirection.Down));
    }

    [Fact]
    public void PreferTheBestAlignedTile_WhenRowsHaveDifferentTileWidths()
    {
        // A row of two wide banners above a row of four narrow posters.
        var tiles = new List<Rect>
        {
            new(0, 0, 240, Height),
            new(260, 0, 240, Height),
            new(0, 80, 110, Height),
            new(130, 80, 110, Height),
            new(260, 80, 110, Height),
            new(390, 80, 110, Height)
        };

        Assert.Equal(1, SpatialNavigator.FindNext(tiles, 5, NavigationDirection.Up));
        Assert.Equal(2, SpatialNavigator.FindNext(tiles, 0, NavigationDirection.Down));
    }
}
