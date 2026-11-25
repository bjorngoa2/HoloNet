using System.Windows;

namespace HoloNet.TvLauncher.Views;

public enum NavigationDirection
{
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// D-pad "spatial navigation" (the model used by Android TV, tvOS and the W3C CSS spatial
/// navigation spec): the next focus target is chosen from where tiles actually sit on screen,
/// not from list arithmetic like "index + columns". That keeps navigation correct with mixed
/// tile sizes (banners next to posters), a short last row, or any future layout change, and
/// it keeps this logic free of any WPF element so it's unit testable.
/// </summary>
public static class SpatialNavigator
{
    // Tiles whose edge gaps differ by less than this count as the same row/column, so tiny
    // layout rounding never makes a farther tile win over the visually adjacent one.
    private const double SameLineTolerance = 1;

    /// <summary>
    /// Returns the index of the tile to focus when moving <paramref name="direction"/> from
    /// <paramref name="currentIndex"/>, or <c>null</c> when nothing lies that way (focus stays
    /// put - TV launchers don't wrap around edges). Picks the nearest tile in that direction,
    /// breaking ties by how well it lines up on the other axis.
    /// </summary>
    public static int? FindNext(IReadOnlyList<Rect> tiles, int currentIndex, NavigationDirection direction)
    {
        if (currentIndex < 0 || currentIndex >= tiles.Count)
            return null;

        var from = tiles[currentIndex];
        var candidates = tiles
            .Select((rect, index) => (Rect: rect, Index: index))
            .Where(c => c.Index != currentIndex && IsInDirection(from, c.Rect, direction))
            .Select(c => (c.Index, Gap: Gap(from, c.Rect, direction), Offset: CrossAxisOffset(from, c.Rect, direction)))
            .ToList();

        if (candidates.Count == 0)
            return null;

        var nearestGap = candidates.Min(c => c.Gap);
        return candidates
            .Where(c => c.Gap - nearestGap < SameLineTolerance)
            .MinBy(c => c.Offset)
            .Index;
    }

    private static bool IsInDirection(Rect from, Rect to, NavigationDirection direction) => direction switch
    {
        NavigationDirection.Left => Center(to).X < Center(from).X && to.Right <= from.Left + SameLineTolerance,
        NavigationDirection.Right => Center(to).X > Center(from).X && to.Left >= from.Right - SameLineTolerance,
        NavigationDirection.Up => Center(to).Y < Center(from).Y && to.Bottom <= from.Top + SameLineTolerance,
        NavigationDirection.Down => Center(to).Y > Center(from).Y && to.Top >= from.Bottom - SameLineTolerance,
        _ => false
    };

    private static double Gap(Rect from, Rect to, NavigationDirection direction) => direction switch
    {
        NavigationDirection.Left => from.Left - to.Right,
        NavigationDirection.Right => to.Left - from.Right,
        NavigationDirection.Up => from.Top - to.Bottom,
        _ => to.Top - from.Bottom
    };

    private static double CrossAxisOffset(Rect from, Rect to, NavigationDirection direction) =>
        direction is NavigationDirection.Left or NavigationDirection.Right
            ? Math.Abs(Center(to).Y - Center(from).Y)
            : Math.Abs(Center(to).X - Center(from).X);

    private static Point Center(Rect rect) => new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
}
