using System.Windows;
using System.Windows.Media;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Where the tile grid scrolls to, and how its edges fade. A grid that fits on screen never
/// scrolls; one that doesn't always puts the focused row at the top - including the last rows,
/// which the grid pads its end for - so rows above it are fully hidden rather than left
/// showing a few pixels of their bottom edge. The edges fade instead of cutting off hard, so
/// the next row's peek at the bottom reads as "more below" and rows glide out of view.
/// </summary>
public static class TileGridScroll
{
    /// <summary>Height of the top fade; less than the space above the focused row's ring.</summary>
    public const double TopFadeHeight = 10;

    /// <summary>Height of the bottom fade, which the peek of the next row dissolves into.</summary>
    public const double BottomFadeHeight = 48;

    /// <summary>
    /// The vertical offset that shows the focused row: 0 when every tile is clear of the bottom
    /// fade (nothing to scroll), otherwise the focused row's top.
    /// </summary>
    /// <param name="rowTop">Top of the focused row, in grid content coordinates.</param>
    /// <param name="contentBottom">Bottom of the lowest tile, in viewport coordinates when unscrolled.</param>
    /// <param name="viewportHeight">Height of the visible grid area.</param>
    public static double OffsetFor(double rowTop, double contentBottom, double viewportHeight) =>
        contentBottom <= viewportHeight - BottomFadeHeight ? 0 : rowTop;

    /// <summary>An opacity mask fading the top and bottom edges of a viewport this tall.</summary>
    public static Brush EdgeFadeMask(double viewportHeight)
    {
        var top = Math.Min(TopFadeHeight / viewportHeight, 0.5);
        var bottom = Math.Max(1 - BottomFadeHeight / viewportHeight, top);
        var mask = new LinearGradientBrush(
            [
                new GradientStop(Colors.Transparent, 0),
                new GradientStop(Colors.Black, top),
                new GradientStop(Colors.Black, bottom),
                new GradientStop(Colors.Transparent, 1)
            ],
            new Point(0, 0),
            new Point(0, 1));
        mask.Freeze();
        return mask;
    }
}
